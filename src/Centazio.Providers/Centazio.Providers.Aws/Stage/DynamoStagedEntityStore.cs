using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Providers.Aws.Stage;

/// <summary>
/// A note on DynamoStagedEntityStore design
/// Hash Key: `SourceSystem|Object`
/// Main Range Key: `DateStaged` used for querying
/// Secondary Range Key (set up as a Global Secondary Index - GSI): `Checksum` used for batch inserts
///   Note: This GSI is setup to project KEYS_ONLY and hence can only be used to query for the existance
///     of this Checksum, no other queries.
///
/// Batch inserting is done by first querying the GSI for all duplicate `SourceSystem|Object` + `Checksums`.
///    We then filter these out before doing a BatchWriteItem operation
/// </summary>
public class DynamoStagedEntityStore(IAmazonDynamoDB client, string table, int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityStore(limit, checksum) {
  
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
        new(table, [ new(AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY, KeyType.HASH), new(AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY, KeyType.RANGE)]) {
            AttributeDefinitions = [ 
              new (AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY, ScalarAttributeType.S), 
              new (AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY, ScalarAttributeType.S)],

            BillingMode = BillingMode.PAY_PER_REQUEST
          })).TableDescription.TableStatus;
    
    while (status != TableStatus.ACTIVE) {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      status = (await Client.DescribeTableAsync(table)).Table.TableStatus;
    }
    
    return this;
  }

  protected override async Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged) {
    var template = staged.First();
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      KeyExpression = new() {
        ExpressionStatement = $"#haskey = :hashval",
        ExpressionAttributeNames = new() {
          { "#haskey", AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY }
        },
        ExpressionAttributeValues = new() {
          { ":hashval", AwsStagedEntityStoreHelpers.ToDynamoHashKey(template.System, template.SystemEntityType) }
        }
      },
      // this is not efficient, but there is no way to use the 'IN'
      //    operator in a KeyExpression, so making Checksum the range key,
      //    either main or in a GSI does not work.
      // Also, IN expression does not work for single item, so change single
      //    item to `=` expression :(
      FilterExpression = staged.Count > 1 
          ? new() {
            ExpressionStatement = $"{nameof(StagedEntity.StagedEntityChecksum)} IN (:checksums)",
            ExpressionAttributeValues = new() {
              { ":checksums", staged.Select(u => u.StagedEntityChecksum.Value).ToList() }
            } 
          } 
          : new() {
            ExpressionStatement = $"{nameof(StagedEntity.StagedEntityChecksum)} = :checksum",
            ExpressionAttributeValues = new() {
              { ":checksum", staged.Single().StagedEntityChecksum.Value }
            }
          }
    };
    var search = Table.LoadTable(client, table).Query(queryconf);
    var results = await search.GetNextSetAsync();
    
    var existing  = results.ToDictionary(d => d[nameof(StagedEntity.StagedEntityChecksum)].AsString());
    var tostage = staged.Where(e => !existing.ContainsKey(e.StagedEntityChecksum)).ToList();
    await tostage
        .Select(e => new WriteRequest(new PutRequest(e.ToDynamoDict())))
        .Chunk()
        .Select(async chunk => await Client.BatchWriteItemAsync(new BatchWriteItemRequest(
            new() { { table, chunk.ToList() }
        })))
        .Synchronous();
    return tostage;
  }
  
  public override async Task<List<StagedEntity>> Update(List<StagedEntity> staged) {
    var uniques = staged.DistinctBy(e => $"{e.System}|{e.SystemEntityType}|{e.StagedEntityChecksum}").ToList();
    await uniques
        .Select(e => new WriteRequest(new PutRequest(e.ToDynamoDict())))
        .Chunk()
        .Select(async chunk => await Client.BatchWriteItemAsync(new BatchWriteItemRequest(new() { { table, chunk.ToList() } })))
        .Synchronous();
    return uniques;
  }
  
  protected override async Task<List<StagedEntity>> GetImpl(DateTime after, SystemName system, SystemEntityType systype, bool incpromoted) {
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      ConsistentRead = true,
      KeyExpression = new() {
        ExpressionStatement = $"#haskey = :hashval AND #rangekey > :rangeval",
        ExpressionAttributeNames = new() {
          { "#haskey", AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY },
          { "#rangekey", AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY },
        },
        ExpressionAttributeValues = new() {
          { ":hashval", AwsStagedEntityStoreHelpers.ToDynamoHashKey(system, systype) },
          { ":rangeval", $"{after:o}|z" }
        }
      },
      FilterExpression = new() {
        ExpressionStatement = $"attribute_not_exists(#ignore_attr)",
        ExpressionAttributeNames = new() {
          { "#ignore_attr", nameof(StagedEntity.IgnoreReason)}
        }
      }
    };
    var search = Table.LoadTable(client, table).Query(queryconf);
    var results = await search.GetNextSetAsync();
    
    return results.AwsDocumentsToDynamoStagedEntities()
        .Where(se => incpromoted || !se.DatePromoted.HasValue)
        .ToList();
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName system, SystemEntityType systype, bool promoted) {
    var filter = new QueryFilter();
    filter.AddCondition(AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY, QueryOperator.Equal, AwsStagedEntityStoreHelpers.ToDynamoHashKey(system, systype));
    filter.AddCondition(promoted ? nameof(StagedEntity.DatePromoted) : AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY, QueryOperator.LessThan, $"{before:o}");
    var queryconf = new QueryOperationConfig { 
      ConsistentRead = true, 
      Filter = filter,
      Select = SelectValues.SpecificAttributes,
      AttributesToGet = [AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY, AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY]
    };
    var tbl = Table.LoadTable(client, table);
    var search = tbl.Query(queryconf);
    var results = await search.GetRemainingAsync();
    
    if (!results.Any()) { return; }
    
    var batch = tbl.CreateBatchWrite();
    GlobalEnumerableExtensionMethods.ForEach(results, d => batch.AddItemToDelete(d));
    await batch.ExecuteAsync();
  }

}

