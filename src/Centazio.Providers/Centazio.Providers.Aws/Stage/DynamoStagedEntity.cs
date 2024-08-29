using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using C = Centazio.Providers.Aws.Stage.DynamoConstants;

namespace Centazio.Providers.Aws.Stage;

public record DynamoStagedEntity(string RangeKey, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore) {
  
  public Dictionary<string, AttributeValue> ToDynamoDict() {
    var dict = new Dictionary<string, AttributeValue> {
      { C.HASH_KEY, new AttributeValue(this.ToDynamoHashKey()) },
      { C.RANGE_KEY, new AttributeValue($"{RangeKey}") },
      { nameof(Data), new AttributeValue(Data) }
    };
    if (Ignore != null) { dict[nameof(Ignore)] = new AttributeValue(Ignore); }
    if (DatePromoted != null) { dict[nameof(DatePromoted)] = new AttributeValue($"{DatePromoted:o}"); }
    return dict;
  }
}

public static class AwsEntityExtensionMethods {
  
  public static DynamoStagedEntity ToDynamoStagedEntity(this StagedEntity se) => 
      se as DynamoStagedEntity ?? 
          new DynamoStagedEntity($"{se.DateStaged:o}|{Guid.NewGuid()}", se.SourceSystem, se.Object, se.DateStaged, se.Data, se.DatePromoted, se.Ignore);
  
  public static string ToDynamoHashKey(this StagedEntity e) => $"{e.SourceSystem.Value}|{e.Object.Value}";

  public static IList<DynamoStagedEntity> AwsDocumentsToDynamoStagedEntities(this IEnumerable<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[C.HASH_KEY].AsString().Split('|');
      var range = d[C.RANGE_KEY].AsString();
      var staged = range.Split('|').First();
      return new DynamoStagedEntity(
          range, 
          system, 
          entity, 
          DateTime.Parse(staged).ToUniversalTime(), 
          d[nameof(StagedEntity.Data)].AsString(), 
          d.ContainsKey(nameof(StagedEntity.DatePromoted)) ? DateTime.Parse(d[nameof(StagedEntity.DatePromoted)].AsString()).ToUniversalTime() : null,
          d.ContainsKey(nameof(StagedEntity.Ignore)) ? d[nameof(StagedEntity.Ignore)].AsString() : null);
    }).ToList();
  }
}