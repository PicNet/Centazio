namespace Centazio.Core.Entities.Ctl;

public record SystemState(SystemName System, LifecycleStage Stage, bool Active, DateTime DateCreated, DateTime? DateUpdated, DateTime? LastStarted, DateTime? LastCompleted, string? LastException);

public record StagedEntity(SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null) {
  
  private readonly string? ignore;
  public string? Ignore { get => ignore; init => ignore = String.IsNullOrWhiteSpace(value) ? null : value; }

  internal StagedEntity CloneNew() => new(SourceSystem, Object, DateStaged, Data, DatePromoted) { Ignore = Ignore };
}