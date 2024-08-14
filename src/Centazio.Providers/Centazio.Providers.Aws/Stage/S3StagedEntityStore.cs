using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Helpers;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Providers.Aws.Stage;

public record S3StagedEntityStoreConfiguration(string Key, string Secret, string BucketName, int Limit);

public class S3StagedEntityStore(S3StagedEntityStoreConfiguration config) : AbstractStagedEntityStore {

  internal const string DATE_PROMOTED_META_KEY = "x-amz-meta-date-promoted";
  internal const string IGNORE_META_KEY = "x-amz-meta-ignore";

  protected readonly IAmazonS3 client = new AmazonS3Client(
      new BasicAWSCredentials(config.Key, config.Secret), 
      new AmazonS3Config { RegionEndpoint = RegionEndpoint.APSoutheast2 });

  public override ValueTask DisposeAsync() {
    client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<S3StagedEntityStore> Initalise() {
    var tables = (await client.ListBucketsAsync()).Buckets.Select(b => b.BucketName);
    if (tables.Contains(config.BucketName, StringComparer.OrdinalIgnoreCase)) return this;

    Log.Debug($"bucket[{config.BucketName}] not found, creating");
    await client.PutBucketAsync(new PutBucketRequest { BucketName = config.BucketName });
    return this;
  }

  protected override async Task<StagedEntity> SaveImpl(StagedEntity se) {
    var s3se = se.ToS3StagedEntity();
    await client.PutObjectAsync(s3se.ToPutObjectRequest(config.BucketName));
    return s3se;
  }

  protected override async Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> ses) {
    var lst = ses.Select(se => se.ToS3StagedEntity()).ToList();
    await lst.Select(s3se => client.PutObjectAsync(s3se.ToPutObjectRequest(config.BucketName))).ChunkedSynchronousCall(5);
    return lst.AsEnumerable();
  }

  public override Task Update(StagedEntity staged) => SaveImpl(staged);
  public override Task Update(IEnumerable<StagedEntity> staged) => SaveImpl(staged);

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    var from = $"{source.Value}/{obj.Value}/{since:o}_z";
    var list = (await ListAll(source, obj))
        .Where(o => String.CompareOrdinal(o.Key, from) > 0) 
        .ToList();
    // note: S3 does not support querying by metadata so 'Ignore' is is handled here
    var objs = await list.Select(i => client.GetObjectAsync(new GetObjectRequest { BucketName = config.BucketName, Key = i.Key }))
          .ChunkedSynchronousCall(5);
    var notignored = objs
          .Where(r => r.Metadata[IGNORE_META_KEY] == null)
          .Take(config.Limit > 0 ? config.Limit : Int32.MaxValue)
          .ToList();
    return (await Task.WhenAll(notignored.Select(r => r.FromS3Response()))).OrderBy(se => se.DateStaged);
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    var beforestr = $"{source.Value}/{obj.Value}/{before:o}";
    var todelete = (await ListAll(source, obj))
        .Where(o => String.CompareOrdinal(o.Key, beforestr) < 0)
        .Select(o => o.Key)
        .ToList();
    // promoted date is always after staged date so we start with `stagedbefore` to reduce the number of objects to query for metadata
    if (todelete.Any() && promoted) {
      var metas = await todelete.Select(key => client.GetObjectMetadataAsync(config.BucketName, key)).ChunkedSynchronousCall(5);
      var mks = metas.Select((m, idx) => (Key: todelete[idx], Meta: m));
      todelete = mks
          .Where(mk => mk.Meta.Metadata[DATE_PROMOTED_META_KEY] != null && String.CompareOrdinal(mk.Meta.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY], $"{before:o}") < 0)
          .Select(mk => mk.Key)
          .ToList();
    }
    if (!todelete.Any()) return;
    
    await client.DeleteObjectsAsync(new DeleteObjectsRequest {
        BucketName = config.BucketName, 
        Objects = todelete.Select(key => new KeyVersion {Key = key }).ToList() });
  }
  
  
  private async Task<List<S3Object>> ListAll(SystemName source, ObjectName obj) =>
      (await client.ListObjectsV2Async(new ListObjectsV2Request { 
        BucketName = config.BucketName, 
        Prefix = $"{source.Value}/{obj.Value}" })).S3Objects;

}

public record S3StagedEntity(Guid KeySuffix, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore) {
  
  public string Key => $"{SourceSystem.Value}/{Object.Value}/{DateStaged:o}_{KeySuffix}";
  
  public PutObjectRequest ToPutObjectRequest(string bucket) {
    var req = new PutObjectRequest { 
      BucketName = bucket, 
      Key = Key, 
      ContentBody = Data
    };
    if (DatePromoted != null) req.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY] = $"{DatePromoted:o}";
    if (Ignore != null) { req.Metadata[S3StagedEntityStore.IGNORE_META_KEY] = Ignore; }
    return req;
  }
}

internal static class S3StagedEntityStore_StagedEntityExtensions {
  
  public static async Task<S3StagedEntity> FromS3Response(this GetObjectResponse r) {
    if (r.Metadata[S3StagedEntityStore.IGNORE_META_KEY] != null) throw new Exception("S3 Objects that are marked as 'Ignore' should not be created as StagedEntities");
    
    var (source, obj, staged_suffix, _) = r.Key.Split('/');
    var (staged, suffix, _) = staged_suffix.Split('_');
    var promoted = r.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY] == null 
        ? (DateTime?) null 
        : DateTime.Parse(r.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY]).ToUniversalTime();
    
    await using var stream = r.ResponseStream;
    using var reader = new StreamReader(stream);
    var data = await reader.ReadToEndAsync();
    
    return new S3StagedEntity(Guid.Parse(suffix), source, obj, DateTime.Parse(staged).ToUniversalTime(), data, promoted);
  }
  
  public static S3StagedEntity ToS3StagedEntity(this StagedEntity se) {
    return se as S3StagedEntity ?? 
        new S3StagedEntity(Guid.NewGuid(), se.SourceSystem, se.Object, se.DateStaged, se.Data, se.DatePromoted, se.Ignore);
  }
}