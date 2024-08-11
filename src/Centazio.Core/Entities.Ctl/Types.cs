namespace Centazio.Core.Entities.Ctl;

public record SystemState(SystemName System, LifecycleStage Stage, bool Active, DateTime DateCreated, DateTime? DateUpdated, DateTime? LastStarted, DateTime? LastCompleted, string? LastException);

public record StagedEntity(SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null) {
  public StagedEntity CloneNew() => new(SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore);
}