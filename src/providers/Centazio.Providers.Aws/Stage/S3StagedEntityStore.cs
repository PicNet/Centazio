using Amazon;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Helpers;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Providers.Aws.Stage;

public record S3StagedEntityStoreConfiguration(string MainBucket);

public class S3StagedEntityStore(string key, string secret, S3StagedEntityStoreConfiguration config) : AbstractStagedEntityStore {

  private readonly IAmazonS3 client = new AmazonS3Client(
      new BasicAWSCredentials(key, secret), 
      new AmazonS3Config { RegionEndpoint = RegionEndpoint.APSoutheast2 });

  public override ValueTask DisposeAsync() {
    client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task Initalise() {
    var tables = (await client.ListBucketsAsync()).Buckets.Select(b => b.BucketName);
    if (!tables.Contains(config.MainBucket, StringComparer.OrdinalIgnoreCase)) {
      Log.Debug($"bucket[{config.MainBucket}] not found, creating");
      await client.PutBucketAsync(new PutBucketRequest { BucketName = config.MainBucket });
    }
  }

  protected override async Task<StagedEntity> SaveImpl(StagedEntity se) {
    await client.PutObjectAsync(new PutObjectRequest {
      BucketName = config.MainBucket,
      Key = se.ToDynamoStagedEntity().RangeKey,
      FilePath = $"{se.SourceSystem}|{se.Object}",
      ContentBody = ""
    });
    return se;
  }

  protected override async Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> ses) {
    var lst = ses.ToList();
    await lst
        .Select(se => new PutObjectRequest { BucketName = config.MainBucket, Key = se.ToDynamoStagedEntity().RangeKey, FilePath = $"{se.SourceSystem}|{se.Object}", ContentBody = "" })
        .Chunk(chunksz: 5)
        .Select(chunk => Task.WhenAll(chunk.Select(req => client.PutObjectAsync(req))))
        .Synchronous();
    return lst.AsEnumerable();
  }

  public override Task Update(StagedEntity staged) => throw new NotImplementedException();
  public override Task Update(IEnumerable<StagedEntity> se) => throw new NotImplementedException();

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    var response = await client.ListObjectsV2Async(new ListObjectsV2Request {
      BucketName = config.MainBucket,
      Prefix = $"{source}|{obj}"
    });
    // todo
    return []; 
  }

  protected override Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) => throw new NotImplementedException();

}

internal static class S3StagedEntityStore_StagedEntityExtensions {
  
  
  public static IList<StagedEntity> AwsBucketsToStagedEntities(this List<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[DynamoStagedEntityStore.KEY_FIELD_NAME].AsString().Split('|');
      return new StagedEntity(system, entity, DateTime.Parse(d[nameof(StagedEntity.DateStaged)]), d[nameof(StagedEntity.Data)], DateTime.Parse(d[nameof(StagedEntity.DatePromoted)])) { Ignore = d[nameof(StagedEntity.Ignore)] };
    }).ToList();
  }
}