using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public record CoreToExternalMap {
  private CoreToExternalMap(
      CoreEntityType coreentity, ValidString coreid, 
      SystemName externalsys, ValidString externalid, 
      EEntityMappingStatus status) {
    CoreEntity = coreentity; 
    CoreId = coreid; 
    ExternalSystem = externalsys; 
    ExternalId = externalid; 
    Status = status;
  }
  
  public record MappingKey(CoreEntityType CoreEntity, ValidString CoreId, SystemName ExternalSystem, ValidString ExternalId);
  
  public MappingKey Key => new(CoreEntity, CoreId, ExternalSystem, ExternalId);
  
  public CoreEntityType CoreEntity { get; } 
  public ValidString CoreId { get; } 
  public SystemName ExternalSystem { get; } 
  public ValidString ExternalId { get; }
  public EEntityMappingStatus Status { get; protected init; }
  
  public DateTime DateCreated { get; protected init; } 
  
  public DateTime? DateUpdated { get; protected init; } 
  public DateTime? DateLastSuccess { get; protected init; } 
  public DateTime? DateLastError { get; protected init; }
  public string? LastError { get; protected init; }
  
  public PendingUpdate Update() => new(this);
  
  public record Dto {
    public string? CoreEntity { get; init; }
    public string? CoreId { get; init; }
    public string? ExternalSystem { get; init; }
    public string? ExternalId { get; init; }
    public string? Status { get; init; }
    public DateTime? DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
    public DateTime? DateLastSuccess { get; init; }
    public DateTime? DateLastError { get; init; }
    public string? LastError { get; init; }
    
    public static explicit operator CoreToExternalMap(Dto dto) => new(
        new CoreEntityType(dto.CoreEntity ?? throw new ArgumentNullException(nameof(CoreEntity))),
        dto.CoreId ?? throw new ArgumentNullException(nameof(CoreId)),
        dto.ExternalSystem ?? throw new ArgumentNullException(nameof(ExternalSystem)),
        dto.ExternalId ?? throw new ArgumentNullException(nameof(ExternalId)),
        Enum.Parse<EEntityMappingStatus>(dto.Status ?? throw new ArgumentNullException(nameof(Status)))) {
      
      DateCreated = dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = dto.DateUpdated,
      DateLastSuccess = dto.DateLastSuccess,
      DateLastError = dto.DateLastError,
      LastError = dto.LastError
    };
  }
  
  public static PendingCreate Create(ICoreEntity e, SystemName externalsys, CoreEntityType obj) => new(e, externalsys, obj);
  
  public record PendingCreate {
    public CoreEntityType CoreEntity { get; } 
    public ValidString CoreId { get; } 
    public SystemName SourceSystem { get; } 
    public ValidString SourceId { get; } 
    public SystemName ExternalSystem { get; } 
    public DateTime DateCreated { get; }
    
    // todo: remove `CoreEntityType obj` use `CoreEntityType.From(e)`
    internal PendingCreate(ICoreEntity e, SystemName externalsys, CoreEntityType obj) {
      CoreEntity = obj;
      CoreId = e.Id;
      SourceSystem = e.SourceSystem;
      SourceId = e.SourceId;
      ExternalSystem = externalsys;
      DateCreated = UtcDate.UtcNow;
    }
    
    public Created SuccessCreate(string targetid) => new(this, targetid);
  }
  
  public record Created : CoreToExternalMap {
    internal Created(PendingCreate e, ValidString targetid) : base(e.CoreEntity, e.CoreId, e.ExternalSystem, targetid, EEntityMappingStatus.SuccessCreate) {
      DateUpdated = UtcDate.UtcNow;
      DateLastSuccess = UtcDate.UtcNow; 
    }
  }

  public record PendingUpdate : CoreToExternalMap {
    internal PendingUpdate(CoreToExternalMap e) : base(e.CoreEntity, e.CoreId, e.ExternalSystem, e.ExternalId, e.Status) {}
    
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

  public record Updated : CoreToExternalMap {
    internal Updated(CoreToExternalMap e) : base(e.CoreEntity, e.CoreId, e.ExternalSystem, e.ExternalId, e.Status) {}
  }
}