using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Centazio.Core.Checksum;

namespace Centazio.Core.Ctl.Entities;

public interface ICoreToSysMap {
  public CoreEntityTypeName CoreEntityTypeName { get; } 
  public CoreEntityId CoreId { get; } 
  public SystemName System { get; } 
  public DateTime DateCreated { get; }
}

public static class Map {
  public record Key(CoreEntityTypeName CoreEntityTypeName, CoreEntityId CoreId, SystemName System, SystemEntityId SystemId);
  
  public static PendingCreate Create(SystemName system, ICoreEntity e) => new(e, system);
  
  public record CoreToSysMap : ICoreToSysMap {
    internal CoreToSysMap(
        CoreEntityTypeName coreentity, CoreEntityId coreid, 
        SystemName system, SystemEntityId sysid, 
        EEntityMappingStatus status, SystemEntityChecksum checksum) {
      CoreEntityTypeName = coreentity; 
      CoreId = coreid; 
      System = system; 
      SystemId = sysid; 
      Status = status;
      SystemEntityChecksum = checksum; 
    }
    
    [JsonIgnore] public Key Key => new(CoreEntityTypeName, CoreId, System, SystemId);
    
    public CoreEntityTypeName CoreEntityTypeName { get; } 
    public CoreEntityId CoreId { get; } 
    public SystemName System { get; } 
    public SystemEntityId SystemId { get; }
    public SystemEntityChecksum SystemEntityChecksum { get; internal init; }
    public EEntityMappingStatus Status { get; internal init; }
    public DateTime DateCreated { get; internal init; } 
    public DateTime DateUpdated { get; internal init; }
    
    public DateTime? DateLastSuccess { get; internal init; } 
    public DateTime? DateLastError { get; internal init; }
    [MaxLength(1024)] public string? LastError { get; internal init; }
    
    public PendingUpdate Update() => new(this) { DateUpdated = UtcDate.UtcNow };
    
    public record Dto : IDto<CoreToSysMap> {
      public string? CoreEntityTypeName { get; init; }
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
      
      public CoreToSysMap ToBase() => new(
          new CoreEntityTypeName(CoreEntityTypeName ?? throw new ArgumentNullException(nameof(CoreEntityTypeName))),
          new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
          new(System ?? throw new ArgumentNullException(nameof(System))),
          new (SystemId ?? throw new ArgumentNullException(nameof(SystemId))),
          Enum.Parse<EEntityMappingStatus>(Status ?? throw new ArgumentNullException(nameof(Status))),
          new (SystemEntityChecksum ?? throw new ArgumentNullException(nameof(SystemEntityChecksum)))) {
        
        DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        DateUpdated = DateUpdated ?? throw new ArgumentNullException(nameof(DateUpdated)),
        DateLastSuccess = DateLastSuccess,
        DateLastError = DateLastError,
        LastError = LastError
      };
    }
  }
  
  public record PendingCreate : ICoreToSysMap {
    public CoreEntityTypeName CoreEntityTypeName { get; } 
    public CoreEntityId CoreId { get; }
    public SystemName System { get; } 
    public DateTime DateCreated { get; }
    
    internal PendingCreate(ICoreEntity e, SystemName system) {
      CoreEntityTypeName = CoreEntityTypeName.From(e);
      CoreId = e.CoreId;
      System = system;
      DateCreated = UtcDate.UtcNow;
    }
    
    public Created SuccessCreate(SystemEntityId systemid, SystemEntityChecksum checksum) => new(this, systemid, checksum) {
      DateCreated = DateCreated,
      DateUpdated = UtcDate.UtcNow,
      DateLastSuccess = UtcDate.UtcNow
    };
  }
  
  public record Created : CoreToSysMap {
    internal Created(PendingCreate e, SystemEntityId systemid, SystemEntityChecksum checksum) : base(e.CoreEntityTypeName, e.CoreId, e.System, systemid, EEntityMappingStatus.SuccessCreate, checksum) {
      DateUpdated = UtcDate.UtcNow;
      DateLastSuccess = UtcDate.UtcNow;
    }
  }

  public record PendingUpdate : CoreToSysMap {
    public PendingUpdate(CoreToSysMap e) : base(e) {}
    
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

  public record Updated : CoreToSysMap {
    public Updated(CoreToSysMap e) : base(e) {}
  }
}

