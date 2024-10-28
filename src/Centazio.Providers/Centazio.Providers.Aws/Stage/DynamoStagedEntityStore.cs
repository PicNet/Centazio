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
/// A note on DynamoStagedEntityRepository design
/// Hash Key: `System|Object`
/// Main Range Key: `DateStaged` used for querying
/// Secondary Range Key (set up as a Global Secondary Index - GSI): `Checksum` used for batch inserts
///   Note: This GSI is setup to project KEYS_ONLY and hence can only be used to query for the existance
///     of this Checksum, no other queries.
///
/// Batch inserting is done by first querying the GSI for all duplicate `System|Object` + `Checksums`.
///    We then filter these out before doing a BatchWriteItem operation
/// </summary>
public class DynamoStagedEntityRepository(IAmazonDynamoDB client, string table, int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityRepository(limit, checksum) {
  
  protected IAmazonDynamoDB Client => client;
  
  public override Task<AbstractStagedEntityRepository> Initialise() => Task.FromResult<AbstractStagedEntityRepository>(this);
  public override ValueTask DisposeAsync() {
    Client.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<IStagedEntityRepository> Initalise() {
    if ((await Client.ListTablesAsync()).TableNames
        .Contains(table, StringComparer.OrdinalIgnoreCase)) return this;

    Log.Debug("creating table {Table}", table);
    
    var status = (await Client.CreateTableAsync(
        new(table, [ new(AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY, KeyType.HASH), new(AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY, KeyType.RANGE)]) {
            AttributeDefinitions = [ 
              new (AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY, ScalarAttributeType.S), 
              new (AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY, ScalarAttributeType.S)],

            BillingMode = BillingMode.PAY_PER_REQUEST
          })).TableDescription.TableStatus;
    
    while (status != TableStatus.ACTIVE) {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      status = (await Client.DescribeTableAsync(table)).Table.TableStatus;
    }
    
    return this;
  }

  protected override async Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    var template = staged.First();
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      KeyExpression = new() {
        ExpressionStatement = $"#haskey = :hashval",
        ExpressionAttributeNames = new() {
          { "#haskey", AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY }
        },
        ExpressionAttributeValues = new() {
          { ":hashval", AwsStagedEntityRepositoryHelpers.ToDynamoHashKey(template.System, template.SystemEntityTypeName) }
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
  
  public override async Task<List<StagedEntity>> UpdateImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    var uniques = staged.DistinctBy(e => $"{e.System}|{e.SystemEntityTypeName}|{e.StagedEntityChecksum}").ToList();
    await uniques
        .Select(e => new WriteRequest(new PutRequest(e.ToDynamoDict())))
        .Chunk()
        .Select(async chunk => await Client.BatchWriteItemAsync(new BatchWriteItemRequest(new() { { table, chunk.ToList() } })))
        .Synchronous();
    return uniques;
  }
  
  protected override async Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) {
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      ConsistentRead = true,
      KeyExpression = new() {
        ExpressionStatement = $"#haskey = :hashval AND #rangekey > :rangeval",
        ExpressionAttributeNames = new() {
          { "#haskey", AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY },
          { "#rangekey", AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY },
        },
        ExpressionAttributeValues = new() {
          { ":hashval", AwsStagedEntityRepositoryHelpers.ToDynamoHashKey(system, systype) },
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

  protected override async Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    var filter = new QueryFilter();
    filter.AddCondition(AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY, QueryOperator.Equal, AwsStagedEntityRepositoryHelpers.ToDynamoHashKey(system, systype));
    filter.AddCondition(promoted ? nameof(StagedEntity.DatePromoted) : AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY, QueryOperator.LessThan, $"{before:o}");
    var queryconf = new QueryOperationConfig { 
      ConsistentRead = true, 
      Filter = filter,
      Select = SelectValues.SpecificAttributes,
      AttributesToGet = [AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY, AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY]
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

