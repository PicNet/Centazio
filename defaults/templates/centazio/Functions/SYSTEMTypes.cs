using System.Text.Json.Serialization;

namespace {{ it.Namespace }};

public static class {{ it.SystemName }}Constants {
  public static readonly SystemName {{ it.SystemName }}SystemName = new ("{{ it.SystemName }}");
  
  public static readonly SystemEntityTypeName {{ it.SystemName }}ExampleEntityName = new(nameof({{ it.SystemName }}ExampleEntity));
}

[IgnoreNamingConventions] 
public record {{ it.SystemName }}ExampleEntity(string id, string name, string date_updated) : ISystemEntity {

  
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { id = newid.Value };
  public object GetChecksumSubset() => new { id, name };

}
