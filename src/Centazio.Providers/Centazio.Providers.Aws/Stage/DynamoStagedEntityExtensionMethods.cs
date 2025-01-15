using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;

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
      return new StagedEntity.Dto {
        Id = Guid.Parse(suffix),
        System = system, 
        SystemEntityTypeName = entity, 
        DateStaged = DateTime.Parse(staged).ToUniversalTime(), 
        Data = d[nameof(StagedEntity.Data)].AsString(),
        StagedEntityChecksum = d[nameof(StagedEntity.StagedEntityChecksum)].AsString(),
        DatePromoted = d.ContainsKey(nameof(StagedEntity.DatePromoted)) ? DateTime.Parse(d[nameof(StagedEntity.DatePromoted)].AsString()).ToUniversalTime() : null,
        IgnoreReason = d.ContainsKey(nameof(StagedEntity.IgnoreReason)) ? d[nameof(StagedEntity.IgnoreReason)].AsString() : null 
      }.ToBase();
    }).ToList();
  }
}