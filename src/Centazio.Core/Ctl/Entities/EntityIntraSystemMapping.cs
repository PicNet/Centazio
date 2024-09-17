using Centazio.Core.CoreRepo;
using Centazio.Core.EntitySysMapping;

namespace Centazio.Core.Ctl.Entities;

public record EntityIntraSystemMapping {
  
  public static EntityIntraSystemMapping Create(CreateEntityIntraSystemMapping op) => Create(op.CoreEntity, op.TargetSystem, op.TargetId, op.Status);
  
  public static EntityIntraSystemMapping CreatePending(ICoreEntity CoreEntity, SystemName TargetSystem) {
    return Create(CoreEntity, TargetSystem, EEntityMappingStatus.Pending.ToString(), EEntityMappingStatus.Pending);
  }
  
  private static EntityIntraSystemMapping Create(ICoreEntity CoreEntity, SystemName TargetSystem, ValidString TargetId, EEntityMappingStatus Status) {
    return new(
        CoreEntity.GetType().Name, 
        CoreEntity.Id, 
        CoreEntity.SourceSystem, 
        CoreEntity.SourceId, 
        TargetSystem, 
        TargetId, 
        Status) {
      DateCreated = UtcDate.UtcNow,
      DateLastSuccess = Status == EEntityMappingStatus.Success ? UtcDate.UtcNow : null
    };
  }
  
  public EntityIntraSystemMapping Success() => this with { Status = EEntityMappingStatus.Success, DateUpdated = UtcDate.UtcNow, DateLastSuccess = UtcDate.UtcNow };
  public EntityIntraSystemMapping Error(string? error) => this with { Status = EEntityMappingStatus.Error, DateUpdated = UtcDate.UtcNow, DateLastError = UtcDate.UtcNow, LastError = error };

  private EntityIntraSystemMapping(ObjectName coreentity, ValidString coreid, SystemName sourcesys, ValidString sourceid, SystemName targetsys, ValidString targetid, EEntityMappingStatus status) {
    CoreEntity = coreentity; 
    CoreId = coreid; 
    SourceSystem = sourcesys; 
    SourceId = sourceid; 
    TargetSystem = targetsys; 
    TargetId = targetid; 
    Status = status; 
  }
  
  public ObjectName CoreEntity { get; } 
  public ValidString CoreId { get; } 
  public SystemName SourceSystem { get; } 
  public ValidString SourceId { get; } 
  public SystemName TargetSystem { get; } 
  public ValidString TargetId { get; } 
  
  public DateTime DateCreated { get; private init; } 
  public EEntityMappingStatus Status { get; private init; }
  public DateTime? DateUpdated { get; private init; } 
  public DateTime? DateLastSuccess { get; private init; } 
  public DateTime? DateLastError { get; private init; }
  public string? LastError { get; private init; }
  
  public record MappingKey(ObjectName CoreEntity, ValidString CoreId, SystemName SourceSystem, ValidString SourceId, SystemName TargetSystem, ValidString TargetId);
  
  public MappingKey Key => new(CoreEntity, CoreId, SourceSystem, SourceId, TargetSystem, TargetId);
  
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
    
    public static explicit operator EntityIntraSystemMapping(Dto dto) => new(
        dto.CoreEntity ?? throw new ArgumentNullException(nameof(CoreEntity)),
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
}