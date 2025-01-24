using System.ComponentModel.DataAnnotations;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Sample;

////////////////////////////////////
// ClickUp System Entities
////////////////////////////////////

[IgnoreNamingConventions] 
public record ClickUpTask(string id, string name, ClickUpTask.Status status, string markdown_description, string date_created, string date_updated, string? date_closed, string? date_done, ClickUpTask.Creator creator) : ISystemEntity {

  public record Status(string status, string type);
  public record Creator(int id, string username);

  public object GetChecksumSubset() => new { id, name, status, markdown_description, date_closed, date_done, creator };
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;

}

////////////////////////////////////
// Google Sheets Entities
////////////////////////////////////

[IgnoreNamingConventions] 
public record GoogleSheetsTaskRow(int Index, string Value) : ISystemEntity {
  
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