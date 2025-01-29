using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Sample;

////////////////////////////////////
// ClickUp System Entities
////////////////////////////////////

[IgnoreNamingConventions] 
public record ClickUpTask(string id, string name, string date_updated) : ISystemEntity {
  
  public object GetChecksumSubset() => new { id, name };
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;

}

////////////////////////////////////
// AppSheet Entities
////////////////////////////////////

[IgnoreNamingConventions] 
public record AppSheetTask : ISystemEntity {
  
  public static AppSheetTask Create(string id, string task) => new() { RowId = id, Task = task };
  
  [JsonPropertyName("Row ID")] public string? RowId { get; set; }
  public string? Task { get; set; }
  
  public object GetChecksumSubset() => new { RowId, Task };
  
  // note: implementors still need to mark these properties as [JsonIgnore]
  //    as the JsonSerialiser ignores these attributes on interfaces
  [JsonIgnore] public SystemEntityId SystemId => new(RowId ?? throw new Exception());
  [JsonIgnore] public DateTime LastUpdatedDate => UtcDate.UtcNow;
  [JsonIgnore] public string DisplayName => Task ?? String.Empty;

}

public record AppSheetTaskId {
  [JsonPropertyName("Row ID")] public string? RowId { get; set; }
}

////////////////////////////////////
// Core Entities
////////////////////////////////////

public record CoreTask : CoreEntityBase {
  [MaxLength(128)] public string Name { get; init; }
  public override string DisplayName => Name;
  
  private CoreTask() { Name = null!; }
  internal CoreTask(CoreEntityId coreid, string name) : base(coreid) {
    Name = name;
  }

  public override object GetChecksumSubset() => new { Name };
  
  public record Dto : Dto<CoreTask> {
    public string? Name { get; init; }
    
    public override CoreTask ToBase() {
      var target = new CoreTask { 
        Name = String.IsNullOrWhiteSpace(Name) ? throw new ArgumentNullException(nameof(Name)) : Name.Trim()
      };
      return FillBaseProperties(target);
    }
  }
}