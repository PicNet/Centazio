namespace Centazio.Core.Ctl.Entities;

public record ObjectState {
  
  public static ObjectState Create(SystemName system, LifecycleStage stage, ObjectName obj, bool active) => new(system, stage, obj, active);
  public ObjectState Success(DateTime start, EOperationAbortVote abort, string message, EResultType restype, int length) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = EOperationResult.Success,
      LastAbortVote = abort,
      LastRunMessage = message,
      LastPayLoadType = restype,
      LastPayLoadLength = length,
      LastSuccessStart = start,
      LastSuccessCompleted = UtcDate.UtcNow
    };
  }
  public ObjectState Error(DateTime start, EOperationAbortVote abort, string message, string? exception) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = EOperationResult.Error,
      LastAbortVote = abort,
      LastRunMessage = message,
      LastPayLoadType = EResultType.Error,
      LastPayLoadLength = 0,
      LastRunException = exception
    };
  }
  public ObjectState SetActive(bool active) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      Active = active
    };
  }
  
  public SystemName System { get; } 
  public LifecycleStage Stage { get; } 
  public ObjectName Object { get; } 
  public bool Active { get; private init; } 
  public DateTime DateCreated { get; private init; } 
  public EOperationResult LastResult { get; private init; } = EOperationResult.Unknown;
  public EOperationAbortVote LastAbortVote { get; private init; } = EOperationAbortVote.Unknown;
  
  private ObjectState(SystemName system, LifecycleStage stage, ObjectName obj, bool active) {
    System = system;
    Stage = stage;
    Object = obj;
    Active = active;
    DateCreated = UtcDate.UtcNow;
  }
  
  public DateTime? DateUpdated { get; private init; } 
  public DateTime? LastStart { get; private init; }
  public DateTime? LastSuccessStart { get; private init; }
  public DateTime? LastCompleted { get; private init; }
  public DateTime? LastSuccessCompleted { get; private init; }
  public string? LastRunMessage { get; private init; } 
  public int? LastPayLoadLength { get; private init; } 
  public string? LastRunException { get; private init; }
  public EResultType LastPayLoadType { get; private init; } = EResultType.Empty;
  
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
    public string? LastPayLoadType { get; init; }
    
    public Dto() { }
    
    internal Dto(SystemName system, LifecycleStage stage, ObjectName obj, bool active) {
      System = system;
      Stage = stage;
      Object = obj;
      Active = active;
      DateCreated = UtcDate.UtcNow;
    }
    
    public static explicit operator Dto(ObjectState os) => new(os.System, os.Stage, os.Object, os.Active) {
      LastResult = os.LastResult.ToString(),
      LastAbortVote = os.LastAbortVote.ToString(),
      DateCreated = os.DateCreated,
      DateUpdated = os.DateUpdated,
      LastStart = os.LastStart,
      LastSuccessStart = os.LastSuccessStart,
      LastCompleted = os.LastCompleted,
      LastSuccessCompleted = os.LastSuccessCompleted,
      LastRunMessage = os.LastRunMessage,
      LastPayLoadLength = os.LastPayLoadLength,
      LastRunException = os.LastRunException,
      LastPayLoadType = os.LastPayLoadType.ToString()
    };
    
    public static explicit operator ObjectState(Dto dto) => new(
        dto.System ?? throw new ArgumentNullException(nameof(System)),
        dto.Stage ?? throw new ArgumentNullException(nameof(Stage)),
        dto.Object ?? throw new ArgumentNullException(nameof(Object)),
        dto.Active ?? throw new ArgumentNullException(nameof(Active))) {
      
      LastResult =  Enum.Parse<EOperationResult>(dto.LastResult ?? throw new ArgumentNullException(nameof(LastResult))),
      LastAbortVote =   Enum.Parse<EOperationAbortVote>(dto.LastAbortVote ?? throw new ArgumentNullException(nameof(LastAbortVote))),
      DateCreated = dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = dto.DateUpdated,
      LastStart = dto.LastStart,
      LastSuccessStart = dto.LastSuccessStart,
      LastCompleted = dto.LastCompleted,
      LastSuccessCompleted = dto.LastSuccessCompleted,
      LastRunMessage = dto.LastRunMessage,
      LastPayLoadLength = dto.LastPayLoadLength,
      LastRunException = dto.LastRunException,
      LastPayLoadType = Enum.Parse<EResultType>(dto.LastPayLoadType ?? throw new ArgumentNullException(nameof(LastPayLoadType)))
    };
  }
}