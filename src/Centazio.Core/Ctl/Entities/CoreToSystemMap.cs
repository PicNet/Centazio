using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public interface ICoreToSystemMap {
  public CoreEntityType CoreEntity { get; } 
  public ValidString CoreId { get; } 
  public SystemName System { get; } 
  public DateTime DateCreated { get; }
}

public static class Map {
  public record Key(CoreEntityType CoreEntity, ValidString CoreId, SystemName System, ValidString SystemId);
  
  // factories
  public static PendingCreate Create(ICoreEntity e, SystemName system) => new(e, system);
  
  public record CoreToSystem : ICoreToSystemMap {
    internal CoreToSystem(
        CoreEntityType coreentity, ValidString coreid, 
        SystemName system, ValidString sysid, 
        EEntityMappingStatus status, SystemEntityChecksum checksum) {
      CoreEntity = coreentity; 
      CoreId = coreid; 
      System = system; 
      SystemId = sysid; 
      Status = status;
      SystemEntityChecksum = checksum;
    }
    
    public Key Key => new(CoreEntity, CoreId, System, SystemId);
    
    public CoreEntityType CoreEntity { get; } 
    public ValidString CoreId { get; } 
    public SystemName System { get; } 
    public ValidString SystemId { get; } // todo: rename to SystemId for consistency
    public SystemEntityChecksum SystemEntityChecksum { get; init; }
    public EEntityMappingStatus Status { get; protected init; }
    public DateTime DateCreated { get; internal init; } 
    
    public DateTime? DateUpdated { get; internal init; } 
    public DateTime? DateLastSuccess { get; internal init; } 
    public DateTime? DateLastError { get; internal init; }
    public string? LastError { get; internal init; }
    
    public PendingUpdate Update() => new(this);
    
    public record Dto {
      public string? CoreEntity { get; init; }
      public string? CoreId { get; init; }
      public string? System { get; init; }
      public string? SystemId { get; init; }
      public string? Status { get; init; }
      public DateTime? DateCreated { get; init; }
      public DateTime? DateUpdated { get; init; }
      public DateTime? DateLastSuccess { get; init; }
      public DateTime? DateLastError { get; init; }
      public string? LastError { get; init; }
      public string? SystemEntityChecksum { get; init; }
      
      public static explicit operator CoreToSystem(Dto dto) => new(
          new CoreEntityType(dto.CoreEntity ?? throw new ArgumentNullException(nameof(CoreEntity))),
          dto.CoreId ?? throw new ArgumentNullException(nameof(CoreId)),
          dto.System ?? throw new ArgumentNullException(nameof(System)),
          dto.SystemId ?? throw new ArgumentNullException(nameof(SystemId)),
          Enum.Parse<EEntityMappingStatus>(dto.Status ?? throw new ArgumentNullException(nameof(Status))),
          new (dto.SystemEntityChecksum ?? throw new ArgumentNullException(nameof(SystemEntityChecksum)))) {
        
        DateCreated = dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        DateUpdated = dto.DateUpdated,
        DateLastSuccess = dto.DateLastSuccess,
        DateLastError = dto.DateLastError,
        LastError = dto.LastError
      };
    }
  }
  
  public record PendingCreate : ICoreToSystemMap {
    public CoreEntityType CoreEntity { get; } 
    public ValidString CoreId { get; }
    public SystemName System { get; } 
    public DateTime DateCreated { get; }
    
    internal PendingCreate(ICoreEntity e, SystemName system) {
      CoreEntity = CoreEntityType.From(e);
      CoreId = e.Id;
      System = system;
      DateCreated = UtcDate.UtcNow;
    }
    
    public Created SuccessCreate(string targetid, SystemEntityChecksum checksum) => new(this, targetid, checksum) {
      DateCreated = DateCreated,
      DateUpdated = UtcDate.UtcNow,
      DateLastSuccess = UtcDate.UtcNow
    };
  }
  
  public record Created : CoreToSystem {
    internal Created(PendingCreate e, ValidString targetid, SystemEntityChecksum checksum) : base(e.CoreEntity, e.CoreId, e.System, targetid, EEntityMappingStatus.SuccessCreate, checksum) {
      DateUpdated = UtcDate.UtcNow;
      DateLastSuccess = UtcDate.UtcNow;
    }
  }

  public record PendingUpdate : CoreToSystem {
    internal PendingUpdate(CoreToSystem e) : base(e.CoreEntity, e.CoreId, e.System, e.SystemId, e.Status, e.SystemEntityChecksum) {}
    
    public Updated SuccessUpdate(SystemEntityChecksum checksum) => new(this with { 
      Status = EEntityMappingStatus.SuccessUpdate, 
      DateUpdated = UtcDate.UtcNow, 
      DateLastSuccess = UtcDate.UtcNow,
      SystemEntityChecksum = checksum
    });
    
    public Updated Error(string? error) => new(this with { 
      Status = EEntityMappingStatus.Error, 
      DateUpdated = UtcDate.UtcNow, 
      DateLastError = UtcDate.UtcNow, 
      LastError = error
    });
  }

  public record Updated : CoreToSystem {
    internal Updated(CoreToSystem e) : base(e.CoreEntity, e.CoreId, e.System, e.SystemId, e.Status, e.SystemEntityChecksum) {}
  }
}

