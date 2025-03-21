using System.Text.Json.Serialization;

namespace Centazio.Sample.AppSheet;

public static class AppSheetConstants {
  public static readonly SystemName AppSheetSystemName = new (nameof(AppSheetSystemName));
  
  public static readonly SystemEntityTypeName AppSheetTaskEntityName = new(nameof(AppSheetTask));
}

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
