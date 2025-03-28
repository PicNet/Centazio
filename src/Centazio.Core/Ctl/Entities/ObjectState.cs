﻿using System.ComponentModel.DataAnnotations;
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
  
  public record Dto : IDto<ObjectState> {
    public string? System { get; init; }
    public string? Stage { get; init; }
    public string? Object { get; init; }
    public bool ObjectIsCoreEntityType { get; init; }
    public bool ObjectIsSystemEntityType { get; init; }
    public bool? Active { get; init; }
    public DateTime? DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
    public string? LastResult { get; init; } 
    public string? LastAbortVote { get; init; }
    public DateTime? NextCheckpoint { get; init; }
    
    public DateTime? LastStart { get; init; }
    public DateTime? LastSuccessStart { get; init; }
    public DateTime? LastSuccessCompleted { get; init; }
    public DateTime? LastCompleted { get; init; }
    public string? LastRunMessage { get; init; }
    public string? LastRunException { get; init; }
    
    public ObjectState ToBase() => new(
        new(System ?? throw new ArgumentNullException(nameof(System))),
        new(Stage ?? throw new ArgumentNullException(nameof(Stage))),
        SafeObjectName(),
        NextCheckpoint ?? throw new ArgumentNullException(nameof(NextCheckpoint)),
        Active ?? throw new ArgumentNullException(nameof(Active))) {
      
      LastResult =  Enum.Parse<EOperationResult>(LastResult ?? throw new ArgumentNullException(nameof(LastResult))),
      LastAbortVote = Enum.Parse<EOperationAbortVote>(LastAbortVote ?? throw new ArgumentNullException(nameof(LastAbortVote))),
      DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = DateUpdated ?? throw new ArgumentNullException(nameof(DateUpdated)),
      
      LastStart = LastStart,
      LastSuccessStart = LastSuccessStart,
      LastCompleted = LastCompleted,
      LastSuccessCompleted = LastSuccessCompleted,
      LastRunMessage = LastRunMessage,
      LastRunException = LastRunException
    };
    
    private ObjectName SafeObjectName() {
      if (String.IsNullOrWhiteSpace(Object)) throw new ArgumentNullException(nameof(Object));
      if (ObjectIsCoreEntityType) return new CoreEntityTypeName(Object);
      if (ObjectIsSystemEntityType) return new SystemEntityTypeName(Object);
      throw new NotSupportedException($"ObjectState.Dto was neither of {nameof(CoreEntityTypeName)} or {nameof(SystemEntityTypeName)}");
    }
  }

  [JsonIgnore] public string LoggableValue => $"{System}/{Stage}/{Object}";

}