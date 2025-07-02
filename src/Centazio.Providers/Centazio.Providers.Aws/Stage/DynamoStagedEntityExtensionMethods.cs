using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core.Misc;
using Centazio.Core.Stage;

namespace Centazio.Providers.Aws.Stage;


public static class DynamoStagedEntityExtensionMethods {

  public static Dictionary<string, AttributeValue> ToDynamoDict(this StagedEntity e) {
    var dict = new Dictionary<string, AttributeValue> {
      { AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY, new AttributeValue(AwsStagedEntityRepositoryHelpers.ToDynamoHashKey(e.System, e.SystemEntityTypeName)) },
      { AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY, new AttributeValue(AwsStagedEntityRepositoryHelpers.ToDynamoRangeKey(e.DateStaged, e.Id)) },
      { nameof(e.StagedEntityChecksum), new AttributeValue(e.StagedEntityChecksum) },
      { nameof(e.Data), new AttributeValue(e.Data) }
    };
    if (e.IgnoreReason is not null) { dict[nameof(e.IgnoreReason)] = new AttributeValue(e.IgnoreReason); }
    if (e.DatePromoted is not null) { dict[nameof(e.DatePromoted)] = new AttributeValue($"{e.DatePromoted:o}"); }
    return dict;
  }
  
  public static List<StagedEntity> AwsDocumentsToDynamoStagedEntities(this List<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[AwsStagedEntityRepositoryHelpers.DYNAMO_HASH_KEY].AsString().Split('|');
      var (staged, suffix, _) = d[AwsStagedEntityRepositoryHelpers.DYNAMO_RANGE_KEY].AsString().Split('|');
      var se = StagedEntity.PrivateCreateWithId(
        Guid.Parse(suffix),
        new (system), 
        new (entity), 
        DateTime.Parse(staged).ToUniversalTime(), 
        new (d[nameof(StagedEntity.Data)].AsString()),
        new (d[nameof(StagedEntity.StagedEntityChecksum)].AsString()));
      if (d.ContainsKey(nameof(StagedEntity.DatePromoted))) se = se.Promote(DateTime.Parse(d[nameof(StagedEntity.DatePromoted)].AsString()).ToUniversalTime());
      if (d.ContainsKey(nameof(StagedEntity.IgnoreReason))) se = se.Ignore(new (d[nameof(StagedEntity.IgnoreReason)].AsString()));
      return se;
    }).ToList();
  }
}