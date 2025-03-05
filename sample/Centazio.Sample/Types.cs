﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;

namespace Centazio.Sample;

////////////////////////////////////
// ClickUp System Entities
////////////////////////////////////

[IgnoreNamingConventions] 
public record ClickUpTask(string id, string name, ClickUpTaskStatus status, string date_updated) : ISystemEntity {
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;
  
  [JsonIgnore] public bool IsCompleted => status.status == SampleConstants.Misc.CLICK_UP_COMPLETE_STATUS;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { id = newid.Value };
  public object GetChecksumSubset() => new { id, name, status };

}

public record ClickUpTaskStatus(string status);

////////////////////////////////////
// AppSheet Entities
////////////////////////////////////

[IgnoreNamingConventions] 
public record AppSheetTask : ISystemEntity {
  
  public static AppSheetTask Create(string id, string task, bool completed) => new() { RowId = id, Task = task, Completed = completed };
  
  [JsonPropertyName("Row ID")] public string? RowId { get; set; }
  public string? Task { get; set; }
  // this property is marked as `[JsonIgnore]` because it does not exist in AppSheet.  It is only here
  //    to provide a different checksum when doing meaningful change comparison before writing to AppSheet.
  //    If ommitted then, `AppSheetWriteFunction` will assume there are no meaningful changes to update
  //    and ignore the write.
  [JsonIgnore] public bool Completed { get; set; }
  
  public SystemEntityId SystemId => new(RowId ?? throw new Exception());
  public DateTime LastUpdatedDate => UtcDate.UtcNow;
  public string DisplayName => Task ?? String.Empty;

  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { RowId = newid.Value };
  public object GetChecksumSubset() => new { RowId, Task, Completed };
  
}

public record AppSheetTaskId {
  [JsonPropertyName("Row ID")] public string? RowId { get; set; }
}

////////////////////////////////////
// Core Entities
////////////////////////////////////

public record CoreTask : CoreEntityBase {
  [MaxLength(128)] public string Name { get; init; }
  public bool Completed { get; set; }
  public override string DisplayName => Name;
  
  private CoreTask() { Name = String.Empty; }
  
  internal CoreTask(CoreEntityId coreid, string name, bool completed) : base(coreid) {
    Name = name;
    Completed = completed;
  }

  public override object GetChecksumSubset() => new { CoreId, Name, Completed };
  
  public record Dto : Dto<CoreTask> {
    public string? Name { get; init; }
    public bool Completed { get; init; }
    
    public override CoreTask ToBase() => new() {
      CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
      Name = String.IsNullOrWhiteSpace(Name) ? throw new ArgumentNullException(nameof(Name)) : Name.Trim(),
      Completed = Completed
    };
  }
}