using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public record EntityIntraSysMap {

  public static PendingCreate Create(ICoreEntity e, SystemName target, CoreEntityType obj) => new(e, target, obj);
  
  private EntityIntraSysMap(CoreEntityType coreentity, ValidString coreid, SystemName sourcesys, ValidString sourceid, SystemName targetsys, ValidString targetid, EEntityMappingStatus status) {
    CoreEntity = coreentity; 
    CoreId = coreid; 
    SourceSystem = sourcesys; 
    SourceId = sourceid; 
    TargetSystem = targetsys; 
    TargetId = targetid; 
    Status = status;
  }
  
  public CoreEntityType CoreEntity { get; } 
  public ValidString CoreId { get; } 
  public SystemName SourceSystem { get; } 
  public ValidString SourceId { get; } 
  public SystemName TargetSystem { get; } 
  public ValidString TargetId { get; protected init; } 
  public EEntityMappingStatus Status { get; protected init; }
  public DateTime DateCreated { get; protected init; } 
  
  public DateTime? DateUpdated { get; protected init; } 
  public DateTime? DateLastSuccess { get; protected init; } 
  public DateTime? DateLastError { get; protected init; }
  public string? LastError { get; protected init; }
  
  public record MappingKey(CoreEntityType CoreEntity, ValidString CoreId, SystemName SourceSystem, ValidString SourceId, SystemName TargetSystem, ValidString TargetId);
  
  public MappingKey Key => new(CoreEntity, CoreId, SourceSystem, SourceId, TargetSystem, TargetId);
  public PendingUpdate Update() => new(this);
  
  public record Dto {
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
    public DateTime? DateLastError { get; init; }
    public string? LastError { get; init; }
    
    public static explicit operator EntityIntraSysMap(Dto dto) => new(
        new CoreEntityType(dto.CoreEntity ?? throw new ArgumentNullException(nameof(CoreEntity))),
        dto.CoreId ?? throw new ArgumentNullException(nameof(CoreId)),
        dto.SourceSystem ?? throw new ArgumentNullException(nameof(SourceSystem)),
        dto.SourceId ?? throw new ArgumentNullException(nameof(SourceId)),
        dto.TargetSystem ?? throw new ArgumentNullException(nameof(TargetSystem)),
        dto.TargetId ?? throw new ArgumentNullException(nameof(TargetId)),
        Enum.Parse<EEntityMappingStatus>(dto.Status ?? throw new ArgumentNullException(nameof(Status)))) {
      
      DateCreated = dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = dto.DateUpdated,
      DateLastSuccess = dto.DateLastSuccess,
      DateLastError = dto.DateLastError,
      LastError = dto.LastError
    };
  }
  
  public record PendingCreate {
    public CoreEntityType CoreEntity { get; } 
    public ValidString CoreId { get; } 
    public SystemName SourceSystem { get; } 
    public ValidString SourceId { get; } 
    public SystemName TargetSystem { get; } 
    public DateTime DateCreated { get; }
    
    internal PendingCreate(ICoreEntity e, SystemName targetsys, CoreEntityType obj) {
      CoreEntity = obj;
      CoreId = e.Id;
      SourceSystem = e.SourceSystem;
      SourceId = e.SourceId;
      TargetSystem = targetsys;
      DateCreated = UtcDate.UtcNow;
    }
    
    public Created SuccessCreate(string targetid) => new(this, targetid);
  }

  public record Created : EntityIntraSysMap {
    internal Created(PendingCreate e, ValidString targetid) : base(e.CoreEntity, e.CoreId, e.SourceSystem, e.SourceId, e.TargetSystem, targetid, EEntityMappingStatus.SuccessCreate) {
      DateUpdated = UtcDate.UtcNow;
      DateLastSuccess = UtcDate.UtcNow; 
    }
  }

  public record PendingUpdate : EntityIntraSysMap {
    internal PendingUpdate(EntityIntraSysMap e) : base(e.CoreEntity, e.CoreId, e.SourceSystem, e.SourceId, e.TargetSystem, e.TargetId, e.Status) {}
    
    public Updated SuccessUpdate() => new(this with { 
      Status = EEntityMappingStatus.SuccessUpdate, 
      DateUpdated = UtcDate.UtcNow, 
      DateLastSuccess = UtcDate.UtcNow 
    });
    
    public Updated Error(string? error) => new(this with { 
      Status = EEntityMappingStatus.Error, 
      DateUpdated = UtcDate.UtcNow, 
      DateLastError = UtcDate.UtcNow, 
      LastError = error
    });
  }

  public record Updated : EntityIntraSysMap {
    internal Updated(EntityIntraSysMap e) : base(e.CoreEntity, e.CoreId, e.SourceSystem, e.SourceId, e.TargetSystem, e.TargetId, e.Status) {}
  }
}