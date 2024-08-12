namespace Centazio.Core.Entities.Ctl;

public record SystemState(SystemName System, LifecycleStage Stage, bool Active, DateTime DateCreated, DateTime? DateUpdated, DateTime? LastStarted, DateTime? LastCompleted = null, string? LastException = null);

public record StagedEntity {
  
  public StagedEntity(SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null) {
    this.SourceSystem = SourceSystem;
    this.Object = Object;
    this.DateStaged = DateStaged;
    this.Data = Data;
    this.DatePromoted = DatePromoted;
    this.Ignore = Ignore;
  }
  
  public SystemName SourceSystem { get; init; }
  public ObjectName Object { get; init; }
  public DateTime DateStaged { get; init; }
  public DateTime? DatePromoted { get; init; }
  
  // need the backing fields to support object initializers and the `with` syntax which cannot have validators
  private readonly string data = "";
  private readonly string? ignore;
  public string Data { get => data; init => data = value.Trim(); }
  public string? Ignore { get => ignore; init => ignore = String.IsNullOrWhiteSpace(value) ? null : value.Trim(); }

  internal StagedEntity CloneNew() => new(SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore);
}