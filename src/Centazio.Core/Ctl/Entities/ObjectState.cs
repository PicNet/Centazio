namespace Centazio.Core.Ctl.Entities;

public record ObjectState<O> where O : ObjectName {
  
  public static ObjectState<O> Create(SystemName system, LifecycleStage stage, O name, bool active = true) => new(system, stage, name, active);
  
  public ObjectState<O> Success(DateTime start, EOperationAbortVote abort, string message) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = EOperationResult.Success,
      LastAbortVote = abort,
      LastRunMessage = message,
      LastSuccessStart = start,
      LastSuccessCompleted = UtcDate.UtcNow
    };
  }
  public ObjectState<O> Error(DateTime start, EOperationAbortVote abort, string message, string? exception) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = EOperationResult.Error,
      LastAbortVote = abort,
      LastRunMessage = message,
      LastRunException = exception
    };
  }
  public ObjectState<O> SetActive(bool active) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      Active = active
    };
  }
  
  public SystemName System { get; } 
  public LifecycleStage Stage { get; } 
  public O Object { get; }
  
  public bool Active { get; private init; } 
  public DateTime DateCreated { get; internal init; } 
  public EOperationResult LastResult { get; internal init; } = EOperationResult.Unknown;
  public EOperationAbortVote LastAbortVote { get; internal init; } = EOperationAbortVote.Unknown;
  
  internal ObjectState(SystemName system, LifecycleStage stage, O obj, bool active) {
    System = system;
    Stage = stage;
    Object = obj;
    Active = active;
    DateCreated = UtcDate.UtcNow;
  }
  
  public DateTime? DateUpdated { get; internal init; } 
  public DateTime? LastStart { get; internal init; }
  public DateTime? LastSuccessStart { get; internal init; }
  public DateTime? LastCompleted { get; internal init; }
  public DateTime? LastSuccessCompleted { get; internal init; }
  public string? LastRunMessage { get; internal init; } 
  public string? LastRunException { get; internal init; }
  
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
    public string? LastRunException { get; init; }
    
    public Dto() { }
    
    internal Dto(SystemName system, LifecycleStage stage, ObjectName obj, bool active) {
      System = system;
      Stage = stage;
      Object = obj.Value;
      Active = active;
      DateCreated = UtcDate.UtcNow;
    }
    
    public static Dto FromObjectState(ObjectState<O> os) => new(os.System, os.Stage, os.Object, os.Active) {
      LastResult = os.LastResult.ToString(),
      LastAbortVote = os.LastAbortVote.ToString(),
      DateCreated = os.DateCreated,
      DateUpdated = os.DateUpdated,
      LastStart = os.LastStart,
      LastSuccessStart = os.LastSuccessStart,
      LastCompleted = os.LastCompleted,
      LastSuccessCompleted = os.LastSuccessCompleted,
      LastRunMessage = os.LastRunMessage,
      LastRunException = os.LastRunException
    };
    
    public ObjectState<T> ToObjectState<T>() where T : ObjectName  => new(
        System ?? throw new ArgumentNullException(nameof(System)),
        Stage ?? throw new ArgumentNullException(nameof(Stage)),
        NameFromString<T>(Object),
        Active ?? throw new ArgumentNullException(nameof(Active))) {
      
      LastResult =  Enum.Parse<EOperationResult>(LastResult ?? throw new ArgumentNullException(nameof(LastResult))),
      LastAbortVote =   Enum.Parse<EOperationAbortVote>(LastAbortVote ?? throw new ArgumentNullException(nameof(LastAbortVote))),
      DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = DateUpdated,
      LastStart = LastStart,
      LastSuccessStart = LastSuccessStart,
      LastCompleted = LastCompleted,
      LastSuccessCompleted = LastSuccessCompleted,
      LastRunMessage = LastRunMessage,
      LastRunException = LastRunException
    };
    
    private T NameFromString<T>(string? name) where T : ObjectName {
      ArgumentException.ThrowIfNullOrWhiteSpace(name);
      return (T) (Activator.CreateInstance(typeof(T), name) ?? throw new Exception());
    }
  }
}