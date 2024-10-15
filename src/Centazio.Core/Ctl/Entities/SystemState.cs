namespace Centazio.Core.Ctl.Entities;

public record SystemState {
  public static SystemState Create(SystemName system, LifecycleStage stage, bool active = true, ESystemStateStatus status = ESystemStateStatus.Idle) => new(system, stage, active, UtcDate.UtcNow, status);
  public SystemState Running() => this with { Status = ESystemStateStatus.Running, DateUpdated = UtcDate.UtcNow };
  public SystemState Completed(DateTime funcstart) => this with { Status = ESystemStateStatus.Idle, LastStarted = funcstart, LastCompleted = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow };
  public SystemState SetActive(bool active) => this with { Active = active, DateUpdated = UtcDate.UtcNow };
      
  private SystemState(
      SystemName system, 
      LifecycleStage stage, 
      bool active, 
      DateTime created,
      ESystemStateStatus status) {
    System = system;
    Stage = stage;
    Active = active;
    DateCreated = created;
    Status = status;
  }
  
  public SystemName System { get; }
  public LifecycleStage Stage { get; }
  public DateTime DateCreated { get; }
  
  public bool Active { get; private init; }
  public ESystemStateStatus Status { get; private init; }
  public DateTime? DateUpdated { get; private init; } 
  public DateTime? LastStarted { get; private init; }
  public DateTime? LastCompleted { get; private init; }
  
  public record Dto : IDto<SystemState> {
    public string? System { get; init; }
    public string? Stage { get; init; }
    public bool? Active { get; init; } 
    public DateTime? DateCreated { get; init; }
    public string? Status { get; init; }
    public DateTime? DateUpdated { get; init; }
    public DateTime? LastStarted { get; init; }
    public DateTime? LastCompleted { get; init; }
    
    public Dto() {}
    
    internal Dto(string? system, string? stage, bool? active, DateTime? created, string? status, DateTime? updated = null, DateTime? laststart = null, DateTime? lastcomplete = null) {
      System = system;
      Stage = stage;
      Active = active; 
      DateCreated = created;
      Status = status;
      DateUpdated = updated;
      LastStarted = laststart;
      LastCompleted = lastcomplete;
    }
    
    public SystemState ToBase() => new(
        System ?? throw new ArgumentNullException(nameof(System)), 
        Stage ?? throw new ArgumentNullException(nameof(Stage)), 
        Active ?? throw new ArgumentNullException(nameof(Active)),
        DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        Enum.Parse<ESystemStateStatus>(Status ?? throw new ArgumentNullException(nameof(Status)))) {
      DateUpdated = DateUpdated,
      LastStarted = LastStarted,
      LastCompleted = LastCompleted
    };
  }
}