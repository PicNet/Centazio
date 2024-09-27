using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

// todo: we may need ObjectState subclasses to support CoreEntityType / ExternalEntityType
public record ObjectState {
  
  public static ObjectState Create<T>(SystemName system, LifecycleStage stage, bool active = true) where T : ICoreEntity => new(system, stage, CoreEntityType.From<T>(), active) {
    CoreEntityType = CoreEntityType.From<T>()
  };
  
  public static ObjectState Create(SystemName system, LifecycleStage stage, CoreEntityType name, bool active = true) => new(system, stage, name, active) {
    CoreEntityType = name
  };
  
  public static ObjectState Create(SystemName system, LifecycleStage stage, ExternalEntityType name, bool active = true) => new(system, stage, name, active) {
    ExternalEntityType = name
  };
  
  public static ObjectState Create(SystemName system, LifecycleStage stage, ObjectName name, bool active = true) => new(system, stage, name, active);
  
  public ObjectState Success(DateTime start, EOperationAbortVote abort, string message) {
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
  public ObjectState Error(DateTime start, EOperationAbortVote abort, string message, string? exception) {
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
  
  private readonly CoreEntityType? cet;
  internal CoreEntityType CoreEntityType { get => cet ?? throw new Exception("CoreEntityTypeName is not specified"); private init => cet = value; }
  
  private readonly ExternalEntityType? eet;
  internal ExternalEntityType ExternalEntityType { get => eet ?? throw new Exception("ExternalEntityType is not specified"); private init => eet = value; }
  
  internal ObjectState(SystemName system, LifecycleStage stage, ObjectName obj, bool active) {
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
  public string? LastRunException { get; private init; }
  
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
      LastRunException = os.LastRunException
    };
    
    public ObjectState ToObjectState(bool iscore) => new(
        System ?? throw new ArgumentNullException(nameof(System)),
        Stage ?? throw new ArgumentNullException(nameof(Stage)),
        new(Object ?? throw new ArgumentNullException(nameof(Object))),
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
      LastRunException = LastRunException,
      
      CoreEntityType = iscore ? new CoreEntityType(Object) : null!,
      ExternalEntityType = iscore ? null! : new ExternalEntityType(Object)
    };
  }
}