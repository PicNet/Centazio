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
using C = Centazio.Providers.Aws.Stage.DynamoConstants; 
    
namespace Centazio.Providers.Aws.Stage;

// dynamo limitations: item: 400kb, batch size: 25
public record DynamoStagedEntityStoreConfiguration(string Key, string Secret, string Table, int Limit=100);

public class DynamoStagedEntityStore(DynamoStagedEntityStoreConfiguration config) : AbstractStagedEntityStore {

  protected readonly IAmazonDynamoDB client = new AmazonDynamoDBClient(
      new BasicAWSCredentials(config.Key, config.Secret), 
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
        new CreateTableRequest(config.Table, [ new(C.HASH_KEY, KeyType.HASH), new(C.RANGE_KEY, KeyType.RANGE) ]) {
          AttributeDefinitions = [ new (C.HASH_KEY, ScalarAttributeType.S), new (C.RANGE_KEY, ScalarAttributeType.S) ],
          BillingMode = BillingMode.PAY_PER_REQUEST 
        })).TableDescription.TableStatus;
    
    while (status != TableStatus.ACTIVE) {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      status = (await client.DescribeTableAsync(config.Table)).Table.TableStatus;
    }
    
    return this;
  }
  
  public override async Task<StagedEntity> Update(StagedEntity se) => await SaveImpl(CheckStagedEntityIsDynamoStagedEntity(se));
  public override async Task<IEnumerable<StagedEntity>> Update(IEnumerable<StagedEntity> staged) => await SaveImpl(staged.Select(CheckStagedEntityIsDynamoStagedEntity));
  
  private DynamoStagedEntity CheckStagedEntityIsDynamoStagedEntity(StagedEntity e) => 
      e as DynamoStagedEntity ?? throw new Exception("Expected StagedEntity to be a DynamoStagedEntity");

  protected override async Task<StagedEntity> SaveImpl(StagedEntity se) {
    var dse = se.ToDynamoStagedEntity();
    await client.PutItemAsync(new PutItemRequest(config.Table, dse.ToDynamoDict()));
    return dse;
  }

  protected override async Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> ses) {
      var dses = ses.Select(se => se.ToDynamoStagedEntity()).ToList();
      await dses
          .Select(dse => new WriteRequest(new PutRequest(dse.ToDynamoDict())))
          .Chunk()
          .Select(chunk => client.BatchWriteItemAsync(new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { config.Table, chunk.ToList() } })))
          .Synchronous();
      return dses;
  }

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    var queryconf = new QueryOperationConfig {
      Limit = config.Limit,
      ConsistentRead = true,
      KeyExpression = new Expression {
        ExpressionStatement = $"#haskey = :hashval AND #rangekey > :rangeval",
        ExpressionAttributeNames = new Dictionary<string, string> {
          { "#haskey", C.HASH_KEY },
          { "#rangekey", C.RANGE_KEY },
        },
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
          { ":hashval", new StagedEntity(source, obj, DateTime.MinValue, "").ToDynamoHashKey() },
          { ":rangeval", $"{since:o}|z" }
        }
      },
      FilterExpression = new Expression {
        ExpressionStatement = $"attribute_not_exists(#ignore_attr)",
        ExpressionAttributeNames = new Dictionary<string, string> {
          { "#ignore_attr", nameof(StagedEntity.Ignore)}
        }
      }
    };
    var search = Table.LoadTable(client, config.Table).Query(queryconf);
    var results = await search.GetNextSetAsync();
    
    return results.AwsDocumentsToDynamoStagedEntities();
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    var table = Table.LoadTable(client, config.Table);
    
    var filter = new QueryFilter();
    filter.AddCondition(C.HASH_KEY, QueryOperator.Equal, new StagedEntity(source, obj, DateTime.MinValue, "").ToDynamoHashKey());
    filter.AddCondition(promoted ? nameof(StagedEntity.DatePromoted) : C.RANGE_KEY, QueryOperator.LessThan, $"{before:o}");
    var queryconf = new QueryOperationConfig { 
      Limit = config.Limit, 
      ConsistentRead = true, 
      Filter = filter,
      Select = SelectValues.SpecificAttributes,
      AttributesToGet = [C.HASH_KEY, C.RANGE_KEY]
    };
    var search = table.Query(queryconf);
    var results = await search.GetRemainingAsync();
    
    if (!results.Any()) { return; }
    
    var batch = table.CreateBatchWrite();
    results.ForEachIdx(d => batch.AddItemToDelete(d));
    await batch.ExecuteAsync();
  }

}

