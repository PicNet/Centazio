namespace Centazio.Core.Ctl.Entities;

public enum EOperationResult { Unknown, Success, Error }
public enum EOperationAbortVote { Unknown, Continue, Abort }
public enum EResultType { Error, Empty, Single, List }
public enum ESystemStateStatus { Idle, Running }
public enum EEntityMappingStatus { 
  Pending, 
  Error, 
  Success,
  Orphaned, // no longer has the 'Core' entitiy that this TSE was originaly created for
  MissingTarget // could not find the 'Target' entity in the target system to create link
}

public record SystemStateRaw {
  public string? System { get; init; }
  public string? Stage { get; init; }
  public bool? Active { get; init; } 
  public DateTime? DateCreated { get; init; }
  public string? Status { get; init; }
  public DateTime? DateUpdated { get; init; }
  public DateTime? LastStarted { get; init; }
  public DateTime? LastCompleted { get; init; }
  
  public static explicit operator SystemState(SystemStateRaw raw) => new(
      raw.System ?? throw new ArgumentNullException(nameof(System)), 
      raw.Stage ?? throw new ArgumentNullException(nameof(Stage)), 
      raw.Active ?? throw new ArgumentNullException(nameof(Active)),
      raw.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      Enum.Parse<ESystemStateStatus>(raw.Status ?? throw new ArgumentNullException(nameof(Status))),
      raw.DateUpdated,
      raw.LastStarted,
      raw.LastCompleted);
}

public record SystemState (
    SystemName System, 
    LifecycleStage Stage, 
    bool Active, 
    DateTime DateCreated,
    ESystemStateStatus Status,
    DateTime? DateUpdated = null, 
    DateTime? LastStarted = null, 
    DateTime? LastCompleted = null);

public record ObjectStateRaw {
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
  
  public static explicit operator ObjectState(ObjectStateRaw raw) => new(
      raw.System ?? throw new ArgumentNullException(nameof(System)),
      raw.Stage ?? throw new ArgumentNullException(nameof(Stage)),
      raw.Object ?? throw new ArgumentNullException(nameof(Object)),
      raw.Active ?? throw new ArgumentNullException(nameof(Active)),
      raw.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      Enum.Parse<EOperationResult>(raw.LastResult ?? throw new ArgumentNullException(nameof(LastResult))),
      Enum.Parse<EOperationAbortVote>(raw.LastAbortVote ?? throw new ArgumentNullException(nameof(LastAbortVote))),
      raw.DateUpdated,
      raw.LastStart,
      raw.LastSuccessStart,
      raw.LastCompleted,
      raw.LastSuccessCompleted,
      raw.LastRunMessage,
      raw.LastPayLoadLength,
      raw.LastRunException);
}

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
}

public record StagedEntity {
  
  public StagedEntity(SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, string Checksum, DateTime? DatePromoted = null, string? Ignore = null) {
    this.SourceSystem = SourceSystem;
    this.Object = Object;
    this.DateStaged = DateStaged;
    this.Data = Data;
    this.Checksum = Checksum;
    this.DatePromoted = DatePromoted;
    this.Ignore = Ignore;
  }
  
  public SystemName SourceSystem { get; init; }
  public ObjectName Object { get; init; }
  public DateTime DateStaged { get; init; }
  public DateTime? DatePromoted { get; init; }
  public string Checksum { get; init; }
  
  // need the backing fields to support object initializers and the `with` syntax which cannot have validators
  private readonly string data = "";
  private readonly string? ignore;
  public string Data { get => data; init => data = value.Trim(); }
  public string? Ignore { get => ignore; init => ignore = String.IsNullOrWhiteSpace(value) ? null : value.Trim(); }

  internal StagedEntity CloneNew() => new(SourceSystem, Object, DateStaged, Data, Checksum, DatePromoted, Ignore);
}

public record EntityIntraSystemMappingRaw {
  public string? CoreEntity { get; init; }
  public string? CoreId { get; init; }
  public string? SourceSystem { get; init; }
  public string? SourceId { get; init; }
  public string? TargetSystem { get; init; }
  public string? TargetId { get; init; }
  public string? Status { get; init; }
  public DateTime? DateCreated { get; init; }
  public DateTime? DateUpdated { get; init; }
  public DateTime? DateLastSuccess { get; init; }
  public string? LastError { get; init; }
  
  public static explicit operator EntityIntraSystemMapping(EntityIntraSystemMappingRaw raw) => new(
      raw.CoreEntity ?? throw new ArgumentNullException(nameof(CoreEntity)),
      raw.CoreId ?? throw new ArgumentNullException(nameof(CoreId)),
      raw.SourceSystem ?? throw new ArgumentNullException(nameof(SourceSystem)),
      raw.SourceId ?? throw new ArgumentNullException(nameof(SourceId)),
      raw.TargetSystem ?? throw new ArgumentNullException(nameof(TargetSystem)),
      raw.TargetId ?? throw new ArgumentNullException(nameof(TargetId)),
      Enum.Parse<EEntityMappingStatus>(raw.Status ?? throw new ArgumentNullException(nameof(Status))),
      raw.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      raw.DateUpdated,
      raw.DateLastSuccess,
      raw.LastError);
}

public record EntityIntraSystemMapping(ObjectName CoreEntity, ValidString CoreId, SystemName SourceSystem, ValidString SourceId, SystemName TargetSystem, ValidString TargetId, EEntityMappingStatus Status, DateTime DateCreated, DateTime? DateUpdated, DateTime? DateLastSuccess, string? LastError) {
  public record MappingKey(ObjectName CoreEntity, ValidString CoreId, SystemName SourceSystem, ValidString SourceId, SystemName TargetSystem, ValidString TargetId);
  
  public MappingKey Key => new MappingKey(CoreEntity, CoreId, SourceSystem, SourceId, TargetSystem, TargetId); 
}
