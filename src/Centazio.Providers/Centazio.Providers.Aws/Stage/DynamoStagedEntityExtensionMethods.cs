using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Providers.Aws.Stage;


public static class DynamoStagedEntityExtensionMethods {

  public static Dictionary<string, AttributeValue> ToDynamoDict(this StagedEntity e) {
    var dse = (DynamoStagedEntity) e;
    var dict = new Dictionary<string, AttributeValue> {
      { DynamoHelpers.HASH_KEY, new AttributeValue(DynamoHelpers.ToHashKey(dse.SourceSystem, dse.Object)) },
      { DynamoHelpers.RANGE_KEY, new AttributeValue(DynamoHelpers.ToRangeKey(dse.DateStaged, dse.Id)) },
      { nameof(e.Checksum), new AttributeValue(e.Checksum) },
      { nameof(e.Data), new AttributeValue(e.Data) }
    };
    if (e.Ignore is not null) { dict[nameof(e.Ignore)] = new AttributeValue(e.Ignore); }
    if (e.DatePromoted is not null) { dict[nameof(e.DatePromoted)] = new AttributeValue($"{e.DatePromoted:o}"); }
    return dict;
  }
  
  public static IList<DynamoStagedEntity> AwsDocumentsToDynamoStagedEntities(this IEnumerable<Document> docs) {
    return docs.Select(d => {
      var (system, entity, _) = d[DynamoHelpers.HASH_KEY].AsString().Split('|');
      var (staged, suffix, _) = d[DynamoHelpers.RANGE_KEY].AsString().Split('|');
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

// todo: is this still required?
public record DynamoStagedEntity(Guid Id, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, string Checksum, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(Id, SourceSystem, Object, DateStaged, Data, Checksum, DatePromoted, Ignore);