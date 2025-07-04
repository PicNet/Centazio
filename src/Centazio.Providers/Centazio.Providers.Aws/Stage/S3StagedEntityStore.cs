using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Providers.Aws.Stage;

public class S3AwsStagedEntityRepository(IAmazonS3 client, string bucket, int limit, Func<string, StagedEntityChecksum> checksum) 
    : AbstractStagedEntityRepository(limit, checksum) {

  internal const string DATE_PROMOTED_META_KEY = "x-amz-meta-date-promoted";
  internal const string IGNORE_META_KEY = "x-amz-meta-ignore";

  protected IAmazonS3 Client => client;

  public override Task<IStagedEntityRepository> Initialise() => Task.FromResult<IStagedEntityRepository>(this);
  public override ValueTask DisposeAsync() {
    Client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<S3AwsStagedEntityRepository> Initalise() {
    var tables = (await Client.ListBucketsAsync()).Buckets?.Select(b => b.BucketName);
    if (tables is not null && tables.Contains(bucket, StringComparer.OrdinalIgnoreCase)) return this;

    Log.Debug("creating bucket {Bucket}", bucket);
    await Client.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
    return this;
  }
  
  protected override async Task<List<StagedEntityChecksum>> GetDuplicateChecksums(SystemName system, SystemEntityTypeName systype, List<StagedEntityChecksum> newchecksums) {
    return (await ListAll(system, systype))
        .Select(o => AwsStagedEntityRepositoryHelpers.ParseS3Key(o.Key).StagedEntityChecksum)
        .ToList();
  }

  protected override async Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> tostage) {
    await tostage.Select(s => Client.PutObjectAsync(ToPutObjectRequest(s))).ChunkedSynchronousCall(5);
    return tostage;
  }
  
  public override async Task UpdateImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    await staged.Select(s => Client.PutObjectAsync(ToPutObjectRequest(s))).ChunkedSynchronousCall(5);
  }

  protected override async Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) {
    var from = $"{system.Value}/{systype.Value}/{after:o}_z";
    var list = (await ListAll(system, systype))
        .Where(o => String.CompareOrdinal(o.Key, from) > 0) 
        .ToList();
    // note: S3 does not support querying by metadata so 'Ignore' is is handled here
    var objs = await list.Select(i => Client.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = i.Key }))
          .ChunkedSynchronousCall(5);
    var notignored = objs
          .Where(r => r.Metadata[IGNORE_META_KEY] is null && (incpromoted || r.Metadata[DATE_PROMOTED_META_KEY] is null))
          .Take(Limit)
          .ToList();
    return (await Task.WhenAll(notignored.Select(r => r.FromS3Response()))).OrderBy(se => se.DateStaged).ToList();
  }

  protected override async Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    var beforestr = $"{system.Value}/{systype.Value}/{before:o}";
    var todelete = (await ListAll(system, systype))
        .Where(o => String.CompareOrdinal(o.Key, beforestr) < 0)
        .Select(o => o.Key)
        .ToList();
    // promoted date is always after staged date so we start with `stagedbefore` to reduce the number of objects to query for metadata
    if (todelete.Any() && promoted) {
      var metas = await todelete.Select(key => Client.GetObjectMetadataAsync(bucket, key)).ChunkedSynchronousCall(5);
      var mks = metas.Select((m, idx) => (Key: todelete[idx], Meta: m));
      todelete = mks
          .Where(mk => mk.Meta.Metadata[DATE_PROMOTED_META_KEY] is not null && String.CompareOrdinal(mk.Meta.Metadata[DATE_PROMOTED_META_KEY], $"{before:o}") < 0)
          .Select(mk => mk.Key)
          .ToList();
    }
    if (!todelete.Any()) return;
    
    await Client.DeleteObjectsAsync(
        new DeleteObjectsRequest { BucketName = bucket, Objects = todelete.Select(key => new KeyVersion {Key = key }).ToList() });
  }
  
  
  private async Task<List<S3Object>> ListAll(SystemName system, SystemEntityTypeName systype) =>
      (await Client.ListObjectsV2Async(new ListObjectsV2Request { 
        BucketName = bucket, 
        Prefix = $"{system.Value}/{systype.Value}" })).S3Objects ?? [];
  
  public PutObjectRequest ToPutObjectRequest(StagedEntity se) {
    var req = new PutObjectRequest { 
      BucketName = bucket, 
      Key = AwsStagedEntityRepositoryHelpers.ToS3Key(se), 
      ContentBody = se.Data
    };
    if (se.DatePromoted is not null) req.Metadata[DATE_PROMOTED_META_KEY] = $"{se.DatePromoted:o}";
    if (se.IgnoreReason is not null) { req.Metadata[IGNORE_META_KEY] = se.IgnoreReason; }
    return req;
  }

}

internal static class S3StagedEntityRepository_StagedEntityExtensions {
  
  public static async Task<StagedEntity> FromS3Response(this GetObjectResponse r) {
    if (r.Metadata[S3AwsStagedEntityRepository.IGNORE_META_KEY] is not null) throw new Exception("S3 objects that are marked as 'Ignore' should not be created");
    var details = AwsStagedEntityRepositoryHelpers.ParseS3Key(r.Key);
    var promoted = r.Metadata[S3AwsStagedEntityRepository.DATE_PROMOTED_META_KEY] is null 
        ? (DateTime?) null 
        : DateTime.Parse(r.Metadata[S3AwsStagedEntityRepository.DATE_PROMOTED_META_KEY]).ToUniversalTime();
    
    await using var stream = r.ResponseStream;
    using var reader = new StreamReader(stream);
    var data = await reader.ReadToEndAsync();
 
    var created = StagedEntity.Create(details.Id, details.System, details.SystemEntityTypeName, details.DateStaged.ToUniversalTime(), new(data), details.StagedEntityChecksum);
    return promoted is null ? created : created.Promote((DateTime) promoted);
  }
}