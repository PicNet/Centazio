using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using C = Centazio.Providers.Aws.Stage.DynamoConstants;

namespace Centazio.Providers.Aws.Stage;


public static class DynamoStagedEntityExtensionMethods {

  public static Dictionary<string, AttributeValue> ToDynamoDict(this StagedEntity e) {
    var dse = (DynamoStagedEntity) e;
    var dict = new Dictionary<string, AttributeValue> {
      { C.HASH_KEY, new AttributeValue(dse.ToHashKey()) },
      { C.RANGE_KEY, new AttributeValue(dse.ToRangeKey()) },
      { nameof(e.Checksum), new AttributeValue(e.Checksum) },
      { nameof(e.Data), new AttributeValue(e.Data) }
    };
    if (e.Ignore is not null) { dict[nameof(e.Ignore)] = new AttributeValue(e.Ignore); }
    if (e.DatePromoted is not null) { dict[nameof(e.DatePromoted)] = new AttributeValue($"{e.DatePromoted:o}"); }
    return dict;
  }
  
  public static string ToHashKey(this StagedEntity e) => $"{e.SourceSystem.Value}|{e.Object.Value}";
  public static string ToRangeKey(this DynamoStagedEntity e) => $"{e.DateStaged:o}|{e.RangeSuffix}";
  
  public static IList<DynamoStagedEntity> AwsDocumentsToDynamoStagedEntities(this IEnumerable<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[C.HASH_KEY].AsString().Split('|');
      var (staged, suffix, _) = d[C.RANGE_KEY].AsString().Split('|');
      return new DynamoStagedEntity(
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

public record DynamoStagedEntity(Guid RangeSuffix, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, string Checksum, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, Checksum, DatePromoted, Ignore);