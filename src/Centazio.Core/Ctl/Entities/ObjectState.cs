using System.Text.Json.Serialization;

namespace Centazio.Core.Ctl.Entities;

public record ObjectState : ILoggable {
  
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
  public ObjectName Object { get; internal init; }
  public bool ObjectIsCoreEntityType { get; }
  public bool ObjectIsSystemEntityType { get; }
  public bool Active { get; private init; } 
  public DateTime DateCreated { get; internal init; } 
  public EOperationResult LastResult { get; internal init; } = EOperationResult.Unknown;
  public EOperationAbortVote LastAbortVote { get; internal init; } = EOperationAbortVote.Unknown;
  
  internal ObjectState(SystemName system, LifecycleStage stage, ObjectName obj, bool active) {
    System = system;
    Stage = stage;
    Object = obj;
    ObjectIsCoreEntityType = obj is CoreEntityTypeName;
    ObjectIsSystemEntityType = obj is SystemEntityTypeName;
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
  
  public record Dto : IDto<ObjectState> {
    public string? System { get; init; }
    public string? Stage { get; init; }
    public string? Object { get; init; }
    public bool ObjectIsCoreEntityType { get; init; }
    public bool ObjectIsSystemEntityType { get; init; }
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
    
    public ObjectState ToBase() => new(
        System ?? throw new ArgumentNullException(nameof(System)),
        Stage ?? throw new ArgumentNullException(nameof(Stage)),
        SafeObjectName(),
        Active ?? throw new ArgumentNullException(nameof(Active))) {
      
      LastResult =  Enum.Parse<EOperationResult>(LastResult ?? throw new ArgumentNullException(nameof(LastResult))),
      LastAbortVote = Enum.Parse<EOperationAbortVote>(LastAbortVote ?? throw new ArgumentNullException(nameof(LastAbortVote))),
      DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = DateUpdated,
      LastStart = LastStart,
      LastSuccessStart = LastSuccessStart,
      LastCompleted = LastCompleted,
      LastSuccessCompleted = LastSuccessCompleted,
      LastRunMessage = LastRunMessage,
      LastRunException = LastRunException
    };
    public object ToBaseAsObj() => ToBase();
    
    private ObjectName SafeObjectName() {
      if (String.IsNullOrWhiteSpace(Object)) throw new ArgumentNullException(nameof(Object));
      if (ObjectIsCoreEntityType) return new CoreEntityTypeName(Object);
      if (ObjectIsSystemEntityType) return new SystemEntityTypeName(Object);
      throw new NotSupportedException($"ObjectState.Dto was neither of {nameof(CoreEntityTypeName)} or {nameof(SystemEntityTypeName)}");
    }
  }

  [JsonIgnore] public string LoggableValue => $"{System}/{Stage}/{Object}";

}