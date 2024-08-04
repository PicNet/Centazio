namespace Centazio.Core.Entities.Ctl;

public record SystemState(SystemName System, LifecycleStage Stage, bool Active, DateTime DateCreated, DateTime? DateUpdated, DateTime? LastStarted, DateTime? LastCompleted, string? LastException);
public record StagedEntity(SystemName Source, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? IgnoreReason = null);