using System.Globalization;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Entities.Ctl;

namespace Centazio.Providers.Aws.Stage;

public record DynamoStagedEntity(string RangeKey, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore) {

  public Dictionary<string, AttributeValue> ToDynamoFullKeyDict() {
    return new Dictionary<string, AttributeValue> {
      { DynamoStagedEntityStore.KEY_FIELD_NAME, new AttributeValue(this.ToDynamoHashKey()) },
      { nameof(RangeKey), new AttributeValue($"{RangeKey}") },
    };
  }
  
  public Dictionary<string, AttributeValue> ToDynamoDict() {
    var dict = new Dictionary<string, AttributeValue> {
      { DynamoStagedEntityStore.KEY_FIELD_NAME, new AttributeValue(this.ToDynamoHashKey()) },
      { nameof(RangeKey), new AttributeValue($"{RangeKey}") },
      { nameof(Data), new AttributeValue(Data) }
    };
    if (Ignore != null) { dict[nameof(Ignore)] = new AttributeValue(Ignore); }
    if (DatePromoted != null) { dict[nameof(DatePromoted)] = new AttributeValue($"{DatePromoted:o}"); }
    return dict;
  }
}

public static class AwsEntityExtensionMethods {
  
  public static DynamoStagedEntity ToDynamoStagedEntity(this StagedEntity se) => new($"{se.DateStaged:o}|{Guid.NewGuid()}", se.SourceSystem, se.Object, se.DateStaged, se.Data, se.DatePromoted, se.Ignore);
  
  public static string ToDynamoHashKey(this StagedEntity e) => $"{e.SourceSystem.Value}|{e.Object.Value}";

  public static Dictionary<string, AttributeValue> ToDynamoDict(this StagedEntity se) => 
      (se as DynamoStagedEntity ?? se.ToDynamoStagedEntity()).ToDynamoDict();

  public static IList<DynamoStagedEntity> AwsDocumentsToDynamoStagedEntities(this IEnumerable<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[DynamoStagedEntityStore.KEY_FIELD_NAME].AsString().Split('|');
      var range = d[nameof(DynamoStagedEntity.RangeKey)].AsString();
      var staged = range.Split('|').First();
      return new DynamoStagedEntity(
          d[nameof(DynamoStagedEntity.RangeKey)].AsString(), 
          system, 
          entity, 
          DateTime.Parse(staged).ToUniversalTime(), 
          d[nameof(StagedEntity.Data)].AsString(), 
          d.ContainsKey(nameof(StagedEntity.DatePromoted)) ? DateTime.Parse(d[nameof(StagedEntity.DatePromoted)].AsString()).ToUniversalTime() : null, 
          d.ContainsKey(nameof(StagedEntity.Ignore)) ? d[nameof(StagedEntity.Ignore)].AsString() : null);
    }).ToList();
  }
}