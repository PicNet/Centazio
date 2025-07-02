using System.ComponentModel.DataAnnotations;

namespace Centazio.Sample.Shared;

public static class CoreEntityTypes {
  public static readonly CoreEntityTypeName Task = new(nameof(CoreTask));
}

public record CoreTask : CoreEntityBase {
  [MaxLength(128)] public string Name { get; init; }
  public bool Completed { get; set; }
  public override string DisplayName => Name;
  
  private CoreTask() { Name = String.Empty; }
  
  public CoreTask(CoreEntityId coreid, string name, bool completed) : base(coreid) {
    Name = name;
    Completed = completed;
  }

  public override object GetChecksumSubset() => new { CoreId, Name, Completed };
}