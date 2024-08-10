using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Helpers;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Providers.Aws.Stage;

public record DynamoStagedEntityStoreConfiguration(string Table, int PageSize=100, int MaxPages=1);

// dynamo limitations:
// item: 400kb
// batch size: 25
public class DynamoStagedEntityStore(string key, string secret, DynamoStagedEntityStoreConfiguration config) : AbstractStagedEntityStore {

  public static readonly string KEY_FIELD_NAME = $"{nameof(StagedEntity.SourceSystem)}|{nameof(StagedEntity.Object)}";
  
  protected readonly IAmazonDynamoDB client = new AmazonDynamoDBClient(
      new BasicAWSCredentials(key, secret), 
      new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });

  public override ValueTask DisposeAsync() {
    client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<IStagedEntityStore> Initalise() {
    if ((await client.ListTablesAsync()).TableNames
        .Contains(config.Table, StringComparer.OrdinalIgnoreCase)) return this;

    Log.Debug($"table[{config.Table}] not found, creating");
    
    await client.CreateTableAsync(new CreateTableRequest(config.Table, [ new(KEY_FIELD_NAME, KeyType.HASH), new(nameof(StagedEntity.DateStaged), KeyType.RANGE) ]) {
      AttributeDefinitions = [ new (KEY_FIELD_NAME, ScalarAttributeType.S), new (nameof(StagedEntity.DateStaged), ScalarAttributeType.S) ],
      BillingMode = BillingMode.PAY_PER_REQUEST 
    });
    return this;
  }
  
  public override async Task Update(StagedEntity se) => await SaveImpl(se);
  public override async Task Update(IEnumerable<StagedEntity> ses) => await SaveImpl(ses);

  protected override async Task SaveImpl(StagedEntity se) => 
      await client.PutItemAsync(new PutItemRequest(config.Table, se.ToDynamoDict()));

  protected override async Task SaveImpl(IEnumerable<StagedEntity> ses) => 
      await ses
          .Select(se => new WriteRequest(new PutRequest(se.ToDynamoDict())))
          .Chunk()
          .Select(chunk => client.BatchWriteItemAsync(new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { config.Table, chunk.ToList() } })))
          .Synchronous();

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    var tbl = Table.LoadTable(client, config.Table);
    var key = new StagedEntity(source, obj, since, "").ToDynamoKey();
    var queryconf = new QueryOperationConfig {
      Limit = config.PageSize,
      ConsistentRead = true,
      KeyExpression = new Expression { ExpressionStatement = $"{KEY_FIELD_NAME}={key}" },
      FilterExpression = new Expression { ExpressionStatement = $"{nameof(StagedEntity.DateStaged)} > {since:o}"}
    };
    var search = tbl.Query(queryconf);
    // todo: implement pagination and use MaxPages
    var results = await search.GetRemainingAsync();
    return results.AwsDocumentsToStagedEntities();
  }

  protected override Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) => throw new NotImplementedException();

}

internal static class DynamoStagedEntityStore_StagedEntityExtensions {
  public static string ToDynamoKey(this StagedEntity e) => $"{e.SourceSystem.Value}|{e.Object.Value}";
  
  public static Dictionary<string, AttributeValue> ToDynamoKeyDict(this StagedEntity e) => new() { 
    { DynamoStagedEntityStore.KEY_FIELD_NAME, new AttributeValue(e.ToDynamoKey()) },
    { nameof(StagedEntity.DateStaged), new AttributeValue($"{e.DateStaged:o}") }
  };
  
  public static Dictionary<string, AttributeValue> ToDynamoDict(this StagedEntity e) => new() { 
    { DynamoStagedEntityStore.KEY_FIELD_NAME, new AttributeValue(e.ToDynamoKey()) },
    { nameof(StagedEntity.DateStaged), new AttributeValue($"{e.DateStaged:o}") },
    { nameof(StagedEntity.Data), new AttributeValue(e.Data) },
    { nameof(StagedEntity.Ignore), new AttributeValue(e.Ignore) },
    { nameof(StagedEntity.DatePromoted), new AttributeValue(e.DatePromoted.HasValue ? $"{e.DatePromoted:o}" : null) },
  };
  
  public static IList<StagedEntity> AwsDocumentsToStagedEntities(this List<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[DynamoStagedEntityStore.KEY_FIELD_NAME].AsString().Split('|');
      return new StagedEntity(system, entity, DateTime.Parse(d[nameof(StagedEntity.DateStaged)]), d[nameof(StagedEntity.Data)], DateTime.Parse(d[nameof(StagedEntity.DatePromoted)]), d[nameof(StagedEntity.Ignore)]);
    }).ToList();
  }
}