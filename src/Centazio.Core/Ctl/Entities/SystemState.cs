namespace Centazio.Core.Ctl.Entities;

public record SystemState (
    SystemName System, 
    LifecycleStage Stage, 
    bool Active, 
    DateTime DateCreated,
    ESystemStateStatus Status,
    DateTime? DateUpdated = null, 
    DateTime? LastStarted = null, 
    DateTime? LastCompleted = null) {
  
  public record Dto {
    public string? System { get; init; }
    public string? Stage { get; init; }
    public bool? Active { get; init; } 
    public DateTime? DateCreated { get; init; }
    public string? Status { get; init; }
    public DateTime? DateUpdated { get; init; }
    public DateTime? LastStarted { get; init; }
    public DateTime? LastCompleted { get; init; }
    
    public static explicit operator SystemState(SystemState.Dto dto) => new(
        dto.System ?? throw new ArgumentNullException(nameof(System)), 
        dto.Stage ?? throw new ArgumentNullException(nameof(Stage)), 
        dto.Active ?? throw new ArgumentNullException(nameof(Active)),
        dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        Enum.Parse<ESystemStateStatus>(dto.Status ?? throw new ArgumentNullException(nameof(Status))),
        dto.DateUpdated,
        dto.LastStarted,
        dto.LastCompleted);
  }
}