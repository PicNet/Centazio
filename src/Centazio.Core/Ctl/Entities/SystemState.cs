namespace Centazio.Core.Ctl.Entities;

public record SystemState {
  public static SystemState Create(SystemName system, LifecycleStage stage, bool active = true, ESystemStateStatus status = ESystemStateStatus.Idle) => new(system, stage, active, UtcDate.UtcNow, UtcDate.UtcNow, status);
  public SystemState Running() => this with { Status = ESystemStateStatus.Running, DateUpdated = UtcDate.UtcNow };
  public SystemState Completed(DateTime funcstart) {
    if (funcstart == DateTime.MinValue) throw new ArgumentNullException(nameof(funcstart));
    return this with { Status = ESystemStateStatus.Idle, LastStarted = funcstart, LastCompleted = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow };
  }

  public SystemState SetActive(bool active) => this with { Active = active, DateUpdated = UtcDate.UtcNow };
      
  private SystemState(
      SystemName system, 
      LifecycleStage stage, 
      bool active, 
      DateTime created,
      DateTime updated,
      ESystemStateStatus status) {
    System = system;
    Stage = stage;
    Active = active;
    DateCreated = created;
    DateUpdated = updated;
    Status = status;
  }
  
  public SystemName System { get; }
  public LifecycleStage Stage { get; }
  public DateTime DateCreated { get; }
  public DateTime DateUpdated { get; private init; }
  
  public bool Active { get; private init; }
  public ESystemStateStatus Status { get; private init; }
  public DateTime? LastStarted { get; private init; }
  public DateTime? LastCompleted { get; private init; }
}