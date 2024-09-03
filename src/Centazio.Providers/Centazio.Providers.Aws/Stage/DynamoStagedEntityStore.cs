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
public class DynamoStagedEntityStore(IAmazonDynamoDB client, string table, int limit, Func<string, string> checksum, Func<string, string>? transform = null) : AbstractStagedEntityStore(limit, checksum, transform) {
  
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
        new(table, [ new(C.HASH_KEY, KeyType.HASH), new(C.RANGE_KEY, KeyType.RANGE)]) {
            AttributeDefinitions = [ 
              new (C.HASH_KEY, ScalarAttributeType.S), 
              new (C.RANGE_KEY, ScalarAttributeType.S)],

            BillingMode = BillingMode.PAY_PER_REQUEST
          })).TableDescription.TableStatus;
    
    while (status != TableStatus.ACTIVE) {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      status = (await Client.DescribeTableAsync(table)).Table.TableStatus;
    }
    
    return this;
  }

  protected override async Task<IEnumerable<StagedEntity>> StageImpl(IEnumerable<StagedEntity> staged) {
    var uniques = staged
        .DistinctBy(e => $"{e.SourceSystem}|{e.Object}|{e.Checksum}")
        .Select(e => new DynamoStagedEntity(Guid.NewGuid(), e.SourceSystem, e.Object, e.DateStaged, e.Data, e.Checksum))
        .ToList();
    if (!uniques.Any()) return uniques;

    Console.WriteLine($"KEY: {uniques.First().ToHashKey()}");
    
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      KeyExpression = new() {
        ExpressionStatement = $"#haskey = :hashval",
        ExpressionAttributeNames = new() {
          { "#haskey", C.HASH_KEY }
        },
        ExpressionAttributeValues = new() {
          { ":hashval", uniques.First().ToHashKey() }
        }
      },
      // this is not efficient, but there is no way to use the 'IN'
      //    operator in a KeyExpression, so making Checksum the range key,
      //    either main or in a GSI does not work.
      // Also, IN expression does not work for single item, so change single
      //    item to `=` expression :(
      FilterExpression = uniques.Count > 1 
          ? new() {
            ExpressionStatement = $"{nameof(StagedEntity.Checksum)} IN (:checksums)",
            ExpressionAttributeValues = new() {
              { ":checksums", uniques.Select(u => u.Checksum).ToList() }
            } 
          } 
          : new() {
            ExpressionStatement = $"{nameof(StagedEntity.Checksum)} = :checksum",
            ExpressionAttributeValues = new() {
              { ":checksum", uniques.Single().Checksum }
            }
          }
    };
    var search = Table.LoadTable(client, table).Query(queryconf);
    var results = await search.GetNextSetAsync();
    
    var existing  = results.ToDictionary(d => d[nameof(StagedEntity.Checksum)].AsString());
    var tostage = uniques.Where(e => !existing.ContainsKey(e.Checksum)).ToList();
    await tostage
        .Select(e => new WriteRequest(new PutRequest(e.ToDynamoDict())))
        .Chunk()
        .Select(async chunk => await Client.BatchWriteItemAsync(new BatchWriteItemRequest(
            new() { { table, chunk.ToList() }
        })))
        .Synchronous();
    Console.WriteLine($"UNIQUES: [{String.Join(',', uniques.Select(e => e.Checksum))}]");
    Console.WriteLine($"EXISTING: [{String.Join(',', existing)}]");
    Console.WriteLine($"TOSTAGE: [{String.Join(',', tostage.Select(e => e.Checksum))}]");
    return tostage;
  }
  
  public override async Task<IEnumerable<StagedEntity>> Update(IEnumerable<StagedEntity> staged) {
    var uniques = staged.DistinctBy(e => $"{e.SourceSystem}|{e.Object}|{e.Checksum}").ToList();
    await uniques
        .Select(e => new WriteRequest(new PutRequest(e.ToDynamoDict())))
        .Chunk()
        .Select(async chunk => await Client.BatchWriteItemAsync(new BatchWriteItemRequest(new() { { table, chunk.ToList() } })))
        .Synchronous();
    return uniques;
  }
  
  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj) {
    var queryconf = new QueryOperationConfig {
      Limit = Limit,
      ConsistentRead = true,
      KeyExpression = new() {
        ExpressionStatement = $"#haskey = :hashval AND #rangekey > :rangeval",
        ExpressionAttributeNames = new() {
          { "#haskey", C.HASH_KEY },
          { "#rangekey", C.RANGE_KEY },
        },
        ExpressionAttributeValues = new() {
          { ":hashval", new StagedEntity(source, obj, DateTime.MinValue, "", "").ToHashKey() },
          { ":rangeval", $"{after:o}|z" }
        }
      },
      FilterExpression = new() {
        ExpressionStatement = $"attribute_not_exists(#ignore_attr)",
        ExpressionAttributeNames = new() {
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
    filter.AddCondition(C.HASH_KEY, QueryOperator.Equal, new StagedEntity(source, obj, DateTime.MinValue, "", "").ToHashKey());
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

