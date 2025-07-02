using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Centazio.Core.Ctl.Entities;

public record ObjectState : ILoggable {
  
  public static ObjectState Create(SystemName system, LifecycleStage stage, ObjectName name, DateTime nextcheckpoint, bool active = true) => new(system, stage, name, nextcheckpoint, active);
  
  public ObjectState Success(DateTime nextcheckpoint, DateTime start, EOperationAbortVote abort, string message) {
    return this with {
      DateUpdated = UtcDate.UtcNow,
      NextCheckpoint = nextcheckpoint,
      
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
      // NextCheckpoint is not changed as the error needs to be managed before moving on
      
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
  public DateTime DateUpdated { get; internal init; }
  public EOperationResult LastResult { get; internal init; } = EOperationResult.Unknown;
  public EOperationAbortVote LastAbortVote { get; internal init; } = EOperationAbortVote.Unknown;
  public DateTime NextCheckpoint { get; internal init; } = DateTime.MinValue;
  
  public DateTime? LastStart { get; internal init; }
  public DateTime? LastSuccessStart { get; internal init; }
  public DateTime? LastCompleted { get; internal init; }
  public DateTime? LastSuccessCompleted { get; internal init; }
  [MaxLength(1024)] public string? LastRunMessage { get; internal init; } 
  [MaxLength(4000)] public string? LastRunException { get; internal init; }
  
  internal ObjectState(SystemName system, LifecycleStage stage, ObjectName obj, DateTime nextcheckpoint, bool active) {
    System = system;
    Stage = stage;
    Object = obj;
    ObjectIsCoreEntityType = obj is CoreEntityTypeName;
    ObjectIsSystemEntityType = obj is SystemEntityTypeName;
    NextCheckpoint = nextcheckpoint;
    Active = active;
    DateCreated = DateUpdated = UtcDate.UtcNow;
  }

  [JsonIgnore] public string LoggableValue => $"{System}/{Stage}/{Object}";
}