using System.Text.Json.Serialization;

namespace CentazioTesting.ClickUp;

public static class ClickUpConstants {
  public static readonly SystemName ClickUpSystemName = new ("ClickUp");
  
  public static readonly SystemEntityTypeName ClickUpExampleEntityName = new(nameof(ClickUpExampleEntity));
}

[IgnoreNamingConventions] 
public record ClickUpExampleEntity(string id, string name, string date_updated) : ISystemEntity {

  
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { id = newid.Value };
  public object GetChecksumSubset() => new { id, name };

}
