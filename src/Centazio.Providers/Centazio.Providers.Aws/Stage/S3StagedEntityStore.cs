using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Providers.Aws.Stage;

public class S3StagedEntityStore(IAmazonS3 client, string bucket, int limit, Func<string, string> checksum) : AbstractStagedEntityStore(limit, checksum) {

  internal const string DATE_PROMOTED_META_KEY = "x-amz-meta-date-promoted";
  internal const string IGNORE_META_KEY = "x-amz-meta-ignore";

  protected IAmazonS3 Client => client;

  public override ValueTask DisposeAsync() {
    Client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<S3StagedEntityStore> Initalise() {
    var tables = (await Client.ListBucketsAsync()).Buckets.Select(b => b.BucketName);
    if (tables.Contains(bucket, StringComparer.OrdinalIgnoreCase)) return this;

    Log.Debug("creating bucket {Bucket}", bucket);
    await Client.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
    return this;
  }
  
  protected override async Task<IEnumerable<StagedEntity>> StageImpl(IEnumerable<StagedEntity> staged) {
    var uniques = staged
        .DistinctBy(e => $"{e.SourceSystem}|{e.Object}|{e.Checksum}")
        .Select(s => s.ToS3StagedEntity())
        .ToList();
    if (!uniques.Any()) return uniques;
    
    var existing = (await ListAll(uniques.First().SourceSystem, uniques.First().Object)).Select(o => o.Key.Split('/').Last().Split('_').Last()).ToDictionary(cs => cs);
    
    var tostage = uniques.Where(s => !existing.ContainsKey(s.Checksum)).ToList();
    await tostage.Select(s3se => Client.PutObjectAsync(s3se.ToPutObjectRequest(bucket))).ChunkedSynchronousCall(5);
    return tostage;
  }
  
  public override async Task Update(IEnumerable<StagedEntity> staged) {
    var lst = staged.Select(se => se.ToS3StagedEntity()).ToList();
    await lst.Select(s3se => Client.PutObjectAsync(s3se.ToPutObjectRequest(bucket))).ChunkedSynchronousCall(5);
  }

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj) {
    var from = $"{source.Value}/{obj.Value}/{after:o}_z";
    var list = (await ListAll(source, obj))
        .Where(o => String.CompareOrdinal(o.Key, from) > 0) 
        .ToList();
    // note: S3 does not support querying by metadata so 'Ignore' is is handled here
    var objs = await list.Select(i => Client.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = i.Key }))
          .ChunkedSynchronousCall(5);
    var notignored = objs
          .Where(r => r.Metadata[IGNORE_META_KEY] is null)
          .Take(Limit)
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
      var metas = await todelete.Select(key => Client.GetObjectMetadataAsync(bucket, key)).ChunkedSynchronousCall(5);
      var mks = metas.Select((m, idx) => (Key: todelete[idx], Meta: m));
      todelete = mks
          .Where(mk => mk.Meta.Metadata[DATE_PROMOTED_META_KEY] is not null && String.CompareOrdinal(mk.Meta.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY], $"{before:o}") < 0)
          .Select(mk => mk.Key)
          .ToList();
    }
    if (!todelete.Any()) return;
    
    await Client.DeleteObjectsAsync(new DeleteObjectsRequest {
        BucketName = bucket, 
        Objects = todelete.Select(key => new KeyVersion {Key = key }).ToList() });
  }
  
  
  private async Task<List<S3Object>> ListAll(SystemName source, ObjectName obj) =>
      (await Client.ListObjectsV2Async(new ListObjectsV2Request { 
        BucketName = bucket, 
        Prefix = $"{source.Value}/{obj.Value}" })).S3Objects;

}

public record S3StagedEntity(SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, string Checksum, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, Checksum, DatePromoted, Ignore) {
  
  public string Key => $"{SourceSystem.Value}/{Object.Value}/{DateStaged:o}_{Checksum}";
  
  public PutObjectRequest ToPutObjectRequest(string bucket) {
    var req = new PutObjectRequest { 
      BucketName = bucket, 
      Key = Key, 
      ContentBody = Data
    };
    if (DatePromoted is not null) req.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY] = $"{DatePromoted:o}";
    if (Ignore is not null) { req.Metadata[S3StagedEntityStore.IGNORE_META_KEY] = Ignore; }
    return req;
  }
}

internal static class S3StagedEntityStore_StagedEntityExtensions {
  
  public static async Task<S3StagedEntity> FromS3Response(this GetObjectResponse r) {
    if (r.Metadata[S3StagedEntityStore.IGNORE_META_KEY] is not null) throw new Exception("S3 objects that are marked as 'Ignore' should not be created");
    
    var (source, obj, staged_checksum, _) = r.Key.Split('/');
    var (staged, checksum, _) = staged_checksum.Split('_');
    var promoted = r.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY] is null 
        ? (DateTime?) null 
        : DateTime.Parse(r.Metadata[S3StagedEntityStore.DATE_PROMOTED_META_KEY]).ToUniversalTime();
    
    await using var stream = r.ResponseStream;
    using var reader = new StreamReader(stream);
    var data = await reader.ReadToEndAsync();
    
    return new S3StagedEntity(source, obj, DateTime.Parse(staged).ToUniversalTime(), data, checksum, promoted);
  }
  
  public static S3StagedEntity ToS3StagedEntity(this StagedEntity se) {
    return se as S3StagedEntity ?? 
        new S3StagedEntity(se.SourceSystem, se.Object, se.DateStaged, se.Data, se.Checksum, se.DatePromoted, se.Ignore);
  }
}