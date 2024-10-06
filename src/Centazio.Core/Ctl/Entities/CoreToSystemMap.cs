using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public interface ICoreToSystemMap {
  public CoreEntityType CoreEntity { get; } 
  public ValidString CoreId { get; } 
  public SystemName System { get; } 
  public DateTime DateCreated { get; }
}

public record CoreToSystemMap : ICoreToSystemMap {
  private CoreToSystemMap(
      CoreEntityType coreentity, ValidString coreid, 
      SystemName externalsys, ValidString externalid, 
      EEntityMappingStatus status, SystemEntityChecksum checksum) {
    CoreEntity = coreentity; 
    CoreId = coreid; 
    System = externalsys; 
    ExternalId = externalid; 
    Status = status;
    Checksum = checksum;
  }
  
  public record MappingKey(CoreEntityType CoreEntity, ValidString CoreId, SystemName ExternalSystem, ValidString ExternalId);
  
  public MappingKey Key => new(CoreEntity, CoreId, System, ExternalId);
  
  public CoreEntityType CoreEntity { get; } 
  public ValidString CoreId { get; } 
  public SystemName System { get; } 
  public ValidString ExternalId { get; }
  public SystemEntityChecksum Checksum { get; init; }
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
    public string? Checksum { get; init; }
    
    public static explicit operator CoreToSystemMap(Dto dto) => new(
        new CoreEntityType(dto.CoreEntity ?? throw new ArgumentNullException(nameof(CoreEntity))),
        dto.CoreId ?? throw new ArgumentNullException(nameof(CoreId)),
        dto.ExternalSystem ?? throw new ArgumentNullException(nameof(ExternalSystem)),
        dto.ExternalId ?? throw new ArgumentNullException(nameof(ExternalId)),
        Enum.Parse<EEntityMappingStatus>(dto.Status ?? throw new ArgumentNullException(nameof(Status))),
        new (dto.Checksum ?? throw new ArgumentNullException(nameof(Checksum)))) {
      
      DateCreated = dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated = dto.DateUpdated,
      DateLastSuccess = dto.DateLastSuccess,
      DateLastError = dto.DateLastError,
      LastError = dto.LastError
    };
  }
  
  public static PendingCreate Create(ICoreEntity e, SystemName externalsys) => new(e, externalsys);
  
  public record PendingCreate : ICoreToSystemMap {
    public CoreEntityType CoreEntity { get; } 
    public ValidString CoreId { get; }
    public SystemName System { get; } 
    public DateTime DateCreated { get; }
    
    internal PendingCreate(ICoreEntity e, SystemName externalsys) {
      CoreEntity = CoreEntityType.From(e);
      CoreId = e.Id;
      System = externalsys;
      DateCreated = UtcDate.UtcNow;
    }
    
    public Created SuccessCreate(string targetid, SystemEntityChecksum checksum) => new(this, targetid, checksum);
  }
  
  public record Created : CoreToSystemMap {
    internal Created(PendingCreate e, ValidString targetid, SystemEntityChecksum checksum) : base(e.CoreEntity, e.CoreId, e.System, targetid, EEntityMappingStatus.SuccessCreate, checksum) {
      DateUpdated = UtcDate.UtcNow;
      DateLastSuccess = UtcDate.UtcNow;
    }
  }

  public record PendingUpdate : CoreToSystemMap {
    internal PendingUpdate(CoreToSystemMap e) : base(e.CoreEntity, e.CoreId, e.System, e.ExternalId, e.Status, new(nameof(PendingUpdate))) {}
    
    public Updated SuccessUpdate(SystemEntityChecksum checksum) => new(this with { 
      Status = EEntityMappingStatus.SuccessUpdate, 
      DateUpdated = UtcDate.UtcNow, 
      DateLastSuccess = UtcDate.UtcNow,
      Checksum = checksum
    });
    
    public Updated Error(string? error) => new(this with { 
      Status = EEntityMappingStatus.Error, 
      DateUpdated = UtcDate.UtcNow, 
      DateLastError = UtcDate.UtcNow, 
      LastError = error
    });
  }

  public record Updated : CoreToSystemMap {
    internal Updated(CoreToSystemMap e) : base(e.CoreEntity, e.CoreId, e.System, e.ExternalId, e.Status, e.Checksum) {}
  }
}