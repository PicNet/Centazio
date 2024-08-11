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

// todo: consider adding TTL to these records, maybe to only after promoted?
public record DynamoStagedEntityStoreConfiguration(string Table, int PageSize=100, int MaxPages=1);

// dynamo limitations: item: 400kb, batch size: 25
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
    
    var status = (await client.CreateTableAsync(
        new CreateTableRequest(config.Table, [ new(KEY_FIELD_NAME, KeyType.HASH), new(nameof(DynamoStagedEntity.RangeKey), KeyType.RANGE) ]) {
          AttributeDefinitions = [ new (KEY_FIELD_NAME, ScalarAttributeType.S), new (nameof(DynamoStagedEntity.RangeKey), ScalarAttributeType.S) ],
          BillingMode = BillingMode.PAY_PER_REQUEST 
        })).TableDescription.TableStatus;
    
    while (status != TableStatus.ACTIVE) {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      status = (await client.DescribeTableAsync(config.Table)).Table.TableStatus;
    }
    
    return this;
  }
  
  public override async Task Update(StagedEntity se) => await SaveImpl(CheckStagedEntityIsDynamoStagedEntity(se));
  public override async Task Update(IEnumerable<StagedEntity> ses) => await SaveImpl(ses.Select(CheckStagedEntityIsDynamoStagedEntity));
  
  private DynamoStagedEntity CheckStagedEntityIsDynamoStagedEntity(StagedEntity e) => 
      e as DynamoStagedEntity ?? throw new Exception("Expected StagedEntity to be a DynamoStagedEntity");

  protected override async Task SaveImpl(StagedEntity se) {
    await client.PutItemAsync(new PutItemRequest(config.Table, se.ToDynamoDict()));
  }

  protected override async Task SaveImpl(IEnumerable<StagedEntity> ses) => 
      await ses
          .Select(se => new WriteRequest(new PutRequest(se.ToDynamoDict())))
          .Chunk()
          .Select(chunk => client.BatchWriteItemAsync(new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { config.Table, chunk.ToList() } })))
          .Synchronous();

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    var filter = new QueryFilter();
    filter.AddCondition(KEY_FIELD_NAME, QueryOperator.Equal, new StagedEntity(source, obj, DateTime.MinValue, "").ToDynamoHashKey());
    filter.AddCondition(nameof(DynamoStagedEntity.RangeKey), QueryOperator.GreaterThan, $"{since:o}|z");
    var queryconf = new QueryOperationConfig {
      Limit = config.PageSize,
      ConsistentRead = true,
      Filter = filter
    };
    var search = Table.LoadTable(client, config.Table).Query(queryconf);
    // todo: implement pagination and use MaxPages
    var results = await search.GetRemainingAsync();
    return results
        .Where(d => !d.ContainsKey(nameof(StagedEntity.Ignore)) || String.IsNullOrEmpty(d[nameof(StagedEntity.Ignore)].AsString()))
        .AwsDocumentsToDynamoStagedEntities();
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    var filter = new QueryFilter();
    filter.AddCondition(KEY_FIELD_NAME, QueryOperator.Equal, new StagedEntity(source, obj, DateTime.MinValue, "").ToDynamoHashKey());
    filter.AddCondition(
        promoted ? nameof(StagedEntity.DatePromoted) : nameof(DynamoStagedEntity.RangeKey), 
        QueryOperator.LessThan, 
        promoted ? $"{before:o}" : $"{before:o}|z");
    var queryconf = new QueryOperationConfig {
      Limit = config.PageSize,
      ConsistentRead = true,
      Filter = filter
    };
    var table = Table.LoadTable(client, config.Table);
    var batch = table.CreateBatchWrite();
    var search = table.Query(queryconf);
    var results = await search.GetRemainingAsync();
    if (results.Any()) {
      // todo: do we need batching
      /*
      await results
          .Chunk()
          .Select(chunk => client.BatchWriteItemAsync(new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { config.Table, chunk.ToList() } })))
          .Synchronous();
      */
      results.ForEachIdx(d => batch.AddItemToDelete(d));
      await batch.ExecuteAsync();
    }
  }

}

