namespace Centazio.Core.Ctl.Entities;

public record EntityIntraSystemMapping(
    ObjectName CoreEntity, 
    ValidString CoreId, 
    SystemName SourceSystem, 
    ValidString SourceId, 
    SystemName TargetSystem, 
    ValidString TargetId, 
    EEntityMappingStatus Status, 
    DateTime DateCreated, 
    DateTime? DateUpdated = null, 
    DateTime? DateLastSuccess = null, 
    DateTime? DateLastError = null,
    string? LastError = null) {
  
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
        Enum.Parse<EEntityMappingStatus>(dto.Status ?? throw new ArgumentNullException(nameof(Status))),
        dto.DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
        dto.DateUpdated,
        dto.DateLastSuccess,
        dto.DateLastError,
        dto.LastError);
  }
}