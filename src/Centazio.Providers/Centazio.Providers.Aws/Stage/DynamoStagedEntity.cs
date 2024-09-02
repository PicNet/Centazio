using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using C = Centazio.Providers.Aws.Stage.DynamoConstants;

namespace Centazio.Providers.Aws.Stage;

public record DynamoStagedEntity(SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, string Checksum, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, Checksum, DatePromoted, Ignore) {
  
  public Dictionary<string, AttributeValue> ToDynamoDict() {
    var dict = new Dictionary<string, AttributeValue> {
      { C.HASH_KEY, new AttributeValue(this.ToDynamoHashKey()) },
      { C.RANGE_KEY, new AttributeValue($"{DateStaged:o}|{Checksum}") },
      { nameof(Data), new AttributeValue(Data) }
    };
    if (Ignore is not null) { dict[nameof(Ignore)] = new AttributeValue(Ignore); }
    if (DatePromoted is not null) { dict[nameof(DatePromoted)] = new AttributeValue($"{DatePromoted:o}"); }
    return dict;
  }
}

public static class AwsEntityExtensionMethods {
  
  public static DynamoStagedEntity ToDynamoStagedEntity(this StagedEntity se) => 
      se as DynamoStagedEntity ?? 
          new DynamoStagedEntity(se.SourceSystem, se.Object, se.DateStaged, se.Data, se.Checksum, se.DatePromoted, se.Ignore);
  
  public static string ToDynamoHashKey(this StagedEntity e) => $"{e.SourceSystem.Value}|{e.Object.Value}";

  public static IList<DynamoStagedEntity> AwsDocumentsToDynamoStagedEntities(this IEnumerable<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[C.HASH_KEY].AsString().Split('|');
      var range = d[C.RANGE_KEY].AsString();
      var (staged, checksum, _) = range.Split('|');
      return new DynamoStagedEntity(
          system, 
          entity, 
          DateTime.Parse(staged).ToUniversalTime(), 
          d[nameof(StagedEntity.Data)].AsString(),
          checksum,
          d.ContainsKey(nameof(StagedEntity.DatePromoted)) ? DateTime.Parse(d[nameof(StagedEntity.DatePromoted)].AsString()).ToUniversalTime() : null,
          d.ContainsKey(nameof(StagedEntity.Ignore)) ? d[nameof(StagedEntity.Ignore)].AsString() : null);
    }).ToList();
  }
}