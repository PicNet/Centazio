using System.Text.Json.Serialization;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public interface ICoreToSystemMap {
  public CoreEntityTypeName CoreEntityTypeName { get; } 
  public CoreEntityId CoreId { get; } 
  public SystemName System { get; } 
  public DateTime DateCreated { get; }
}

public static class Map {
  public record Key(CoreEntityTypeName CoreEntityTypeName, CoreEntityId CoreId, SystemName System, SystemEntityId SystemId);
  
  // factories
  public static PendingCreate Create(SystemName system, ICoreEntity e) => new(e, system);
  
  public record CoreToSystem : ICoreToSystemMap {
    internal CoreToSystem(
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
    
    public DateTime? DateUpdated { get; internal init; } 
    public DateTime? DateLastSuccess { get; internal init; } 
    public DateTime? DateLastError { get; internal init; }
    public string? LastError { get; internal init; }
    
    public PendingUpdate Update() => new(this);
    
    public record Dto : IDto<CoreToSystem> {
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
      
      public CoreToSystem ToBase() => new(
          new CoreEntityTypeName(CoreEntityTypeName ?? throw new ArgumentNullException(nameof(CoreEntityTypeName))),
          new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
          System ?? throw new ArgumentNullException(nameof(System)),
          new (SystemId ?? throw new ArgumentNullException(nameof(SystemId))),
          Enum.Parse<EEntityMappingStatus>(Status ?? throw new ArgumentNullException(nameof(Status))),
          new (SystemEntityChecksum ?? throw new ArgumentNullException(nameof(SystemEntityChecksum)))) {
        
        DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        DateUpdated = DateUpdated,
        DateLastSuccess = DateLastSuccess,
        DateLastError = DateLastError,
        LastError = LastError
      };
    }
  }
  
  public record PendingCreate : ICoreToSystemMap {
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
  
  public record Created : CoreToSystem {
    internal Created(PendingCreate e, SystemEntityId systemid, SystemEntityChecksum checksum) : base(e.CoreEntityTypeName, e.CoreId, e.System, systemid, EEntityMappingStatus.SuccessCreate, checksum) {
      DateUpdated = UtcDate.UtcNow;
      DateLastSuccess = UtcDate.UtcNow;
    }
  }

  public record PendingUpdate : CoreToSystem {
    internal PendingUpdate(CoreToSystem e) : base(e.CoreEntityTypeName, e.CoreId, e.System, e.SystemId, e.Status, e.SystemEntityChecksum) {}
    
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
    internal Updated(CoreToSystem e) : base(e.CoreEntityTypeName, e.CoreId, e.System, e.SystemId, e.Status, e.SystemEntityChecksum) {}
  }
}

