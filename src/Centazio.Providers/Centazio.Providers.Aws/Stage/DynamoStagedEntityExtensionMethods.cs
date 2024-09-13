using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Providers.Aws.Stage;


public static class DynamoStagedEntityExtensionMethods {

  public static Dictionary<string, AttributeValue> ToDynamoDict(this StagedEntity e) {
    var dict = new Dictionary<string, AttributeValue> {
      { AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY, new AttributeValue(AwsStagedEntityStoreHelpers.ToDynamoHashKey(e.SourceSystem, e.Object)) },
      { AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY, new AttributeValue(AwsStagedEntityStoreHelpers.ToDynamoRangeKey(e.DateStaged, e.Id)) },
      { nameof(e.Checksum), new AttributeValue(e.Checksum) },
      { nameof(e.Data), new AttributeValue(e.Data) }
    };
    if (e.Ignore is not null) { dict[nameof(e.Ignore)] = new AttributeValue(e.Ignore); }
    if (e.DatePromoted is not null) { dict[nameof(e.DatePromoted)] = new AttributeValue($"{e.DatePromoted:o}"); }
    return dict;
  }
  
  public static IList<StagedEntity> AwsDocumentsToDynamoStagedEntities(this IEnumerable<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[AwsStagedEntityStoreHelpers.DYNAMO_HASH_KEY].AsString().Split('|');
      var (staged, suffix, _) = d[AwsStagedEntityStoreHelpers.DYNAMO_RANGE_KEY].AsString().Split('|');
      return new StagedEntity(
          Guid.Parse(suffix),
          system, 
          entity, 
          DateTime.Parse(staged).ToUniversalTime(), 
          d[nameof(StagedEntity.Data)].AsString(),
          d[nameof(StagedEntity.Checksum)].AsString(),
          d.ContainsKey(nameof(StagedEntity.DatePromoted)) ? DateTime.Parse(d[nameof(StagedEntity.DatePromoted)].AsString()).ToUniversalTime() : null,
          d.ContainsKey(nameof(StagedEntity.Ignore)) ? d[nameof(StagedEntity.Ignore)].AsString() : null);
      }).ToList();
  }
}