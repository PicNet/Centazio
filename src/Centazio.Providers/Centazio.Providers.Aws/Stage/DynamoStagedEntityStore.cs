using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
using Centazio.Core.Stage;
using Serilog;
using C = Centazio.Providers.Aws.Stage.DynamoConstants; 
    
namespace Centazio.Providers.Aws.Stage;

public class DynamoStagedEntityStore(IAmazonDynamoDB client, string table, int limit, Func<string, string> checksum) : AbstractStagedEntityStore(limit, checksum) {
  
  protected IAmazonDynamoDB Client => client;
  
  public override ValueTask DisposeAsync() {
    Client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<IStagedEntityStore> Initalise() {
    if ((await Client.ListTablesAsync()).TableNames
        .Contains(table, StringComparer.OrdinalIgnoreCase)) return this;

    Log.Debug("creating table {Table}", table);
    
    var status = (await Client.CreateTableAsync(
        new CreateTableRequest(table, [ new(C.HASH_KEY, KeyType.HASH), new(C.RANGE_KEY, KeyType.RANGE) ]) {
          AttributeDefinitions = [ new (C.HASH_KEY, ScalarAttributeType.S), new (C.RANGE_KEY, ScalarAttributeType.S) ],
          BillingMode = BillingMode.PAY_PER_REQUEST 
        })).TableDescription.TableStatus;
    
    while (status != TableStatus.ACTIVE) {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      status = (await Client.DescribeTableAsync(table)).Table.TableStatus;
    }
    
    return this;
  }
  
  public override async Task<StagedEntity> Update(StagedEntity se) => await SaveImpl(CheckStagedEntityIsDynamoStagedEntity(se));
  public override async Task<IEnumerable<StagedEntity>> Update(IEnumerable<StagedEntity> staged) => await SaveImpl(staged.Select(CheckStagedEntityIsDynamoStagedEntity));
  
  private DynamoStagedEntity CheckStagedEntityIsDynamoStagedEntity(StagedEntity e) => 
      e as DynamoStagedEntity ?? throw new Exception("Expected StagedEntity to be a DynamoStagedEntity");

  protected override async Task<StagedEntity> SaveImpl(StagedEntity staged) {
    var dse = staged.ToDynamoStagedEntity();
    await Client.PutItemAsync(new PutItemRequest(table, dse.ToDynamoDict()));
    return dse;
  }

  protected override async Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> staged) {
      var dses = staged.Select(se => se.ToDynamoStagedEntity()).ToList();
      await dses
          .Select(dse => new WriteRequest(new PutRequest(dse.ToDynamoDict())))
          .Chunk()
          .Select(chunk => Client.BatchWriteItemAsync(new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> { { table, chunk.ToList() } })))
          .Synchronous();
      return dses;
  }

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj) {
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      ConsistentRead = true,
      KeyExpression = new Expression {
        ExpressionStatement = $"#haskey = :hashval AND #rangekey > :rangeval",
        ExpressionAttributeNames = new Dictionary<string, string> {
          { "#haskey", C.HASH_KEY },
          { "#rangekey", C.RANGE_KEY },
        },
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
          { ":hashval", new StagedEntity(source, obj, DateTime.MinValue, "", "").ToDynamoHashKey() },
          { ":rangeval", $"{after:o}|z" }
        }
      },
      FilterExpression = new Expression {
        ExpressionStatement = $"attribute_not_exists(#ignore_attr)",
        ExpressionAttributeNames = new Dictionary<string, string> {
          { "#ignore_attr", nameof(StagedEntity.Ignore)}
        }
      }
    };
    var search = Table.LoadTable(client, table).Query(queryconf);
    var results = await search.GetNextSetAsync();
    
    return results.AwsDocumentsToDynamoStagedEntities();
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    var filter = new QueryFilter();
    filter.AddCondition(C.HASH_KEY, QueryOperator.Equal, new StagedEntity(source, obj, DateTime.MinValue, "", "").ToDynamoHashKey());
    filter.AddCondition(promoted ? nameof(StagedEntity.DatePromoted) : C.RANGE_KEY, QueryOperator.LessThan, $"{before:o}");
    var queryconf = new QueryOperationConfig { 
      ConsistentRead = true, 
      Filter = filter,
      Select = SelectValues.SpecificAttributes,
      AttributesToGet = [C.HASH_KEY, C.RANGE_KEY]
    };
    var tbl = Table.LoadTable(client, table);
    var search = tbl.Query(queryconf);
    var results = await search.GetRemainingAsync();
    
    if (!results.Any()) { return; }
    
    var batch = tbl.CreateBatchWrite();
    results.ForEachIdx(d => batch.AddItemToDelete(d));
    await batch.ExecuteAsync();
  }

}

