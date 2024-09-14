namespace Centazio.Core.Ctl.Entities;

public record ObjectState(
    SystemName System, 
    LifecycleStage Stage, 
    ObjectName Object, 
    bool Active, 
    DateTime DateCreated, 
    EOperationResult LastResult = EOperationResult.Unknown, 
    EOperationAbortVote LastAbortVote = EOperationAbortVote.Unknown, 
    DateTime? DateUpdated = null, 
    DateTime? LastStart = null,
    DateTime? LastSuccessStart = null,
    DateTime? LastCompleted = null,
    DateTime? LastSuccessCompleted = null,
    string? LastRunMessage = null, 
    int? LastPayLoadLength = null, 
    string? LastRunException = null) {
  public EResultType LastPayLoadType { get; init; }
  
  public record Dto {
    public string? System { get; init; }
    public string? Stage { get; init; }
    public string? Object { get; init; }
    public bool? Active { get; init; }
    public DateTime? DateCreated { get; init; }
    public string? LastResult { get; init; } 
    public string? LastAbortVote { get; init; }  
    public DateTime? DateUpdated { get; init; }
    public DateTime? LastStart { get; init; }
    public DateTime? LastSuccessStart { get; init; }
    public DateTime? LastSuccessCompleted { get; init; }
    public DateTime? LastCompleted { get; init; }
    public string? LastRunMessage { get; init; }
    public int? LastPayLoadLength { get; init; }
    public string? LastRunException { get; init; }
    
    public static explicit operator ObjectState(ObjectState.Dto dto) => new(
        dto.System ?? throw new ArgumentNullException(nameof(System)),
        dto.Stage ?? throw new ArgumentNullException(nameof(Stage)),
        dto.Object ?? throw new ArgumentNullException(nameof(Object)),
        dto.Active ?? throw new ArgumentNullException(nameof(Active)),
        dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        Enum.Parse<EOperationResult>(dto.LastResult ?? throw new ArgumentNullException(nameof(LastResult))),
        Enum.Parse<EOperationAbortVote>(dto.LastAbortVote ?? throw new ArgumentNullException(nameof(LastAbortVote))),
        dto.DateUpdated,
        dto.LastStart,
        dto.LastSuccessStart,
        dto.LastCompleted,
        dto.LastSuccessCompleted,
        dto.LastRunMessage,
        dto.LastPayLoadLength,
        dto.LastRunException);
  }
}