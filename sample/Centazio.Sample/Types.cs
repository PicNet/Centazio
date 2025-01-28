using System.ComponentModel.DataAnnotations;
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
public record AppSheetTaskRow(int Index, string Value) : ISystemEntity {
  
  public object GetChecksumSubset() => new { Index, Row = Value };
  
  public SystemEntityId SystemId { get; } = new(Index.ToString());
  public DateTime LastUpdatedDate => UtcDate.UtcNow;
  public string DisplayName => Value;

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
        Name = new(Name ?? throw new ArgumentNullException(nameof(Name)))
      };
      return FillBaseProperties(target);
    }
  }
}