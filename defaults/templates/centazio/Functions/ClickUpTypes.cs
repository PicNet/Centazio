using System.Text.Json.Serialization;

namespace Centazio.Sample.ClickUp;

public static class ClickUpConstants {
  public static readonly SystemName ClickUpSystemName = new (nameof(ClickUpSystemName));
  
  public static readonly SystemEntityTypeName ClickUpTaskEntityName = new(nameof(ClickUpTask));
}

[IgnoreNamingConventions] 
public record ClickUpTask(string id, string name, ClickUpTaskStatus status, string date_updated) : ISystemEntity {

  
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;
  
  [JsonIgnore] public bool IsCompleted => status.status == ClickUpApi.CLICK_UP_COMPLETE_STATUS;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { id = newid.Value };
  public object GetChecksumSubset() => new { id, name, status };

}

public record ClickUpTaskStatus(string status);
