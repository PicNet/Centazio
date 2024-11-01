using Centazio.Core.Checksum;
using Centazio.Core.Misc;

namespace Centazio.Core.CoreRepo;

public record CoreStorageMeta(SystemName OriginalSystem, SystemEntityId OriginalSystemId, CoreEntityTypeName CoreEntityTypeName, CoreEntityId CoreId, CoreEntityChecksum CoreEntityChecksum, DateTime DateCreated, DateTime DateUpdated, SystemName? LastUpdateSystem) {
  public record Dto : IDto<CoreStorageMeta> {
    public string CoreId { get; init; } = null!;
    
    public string? OriginalSystem { get; init; }
    public string? OriginalSystemId { get; init; }
    public string? CoreEntityTypeName { get; init; }
    public DateTime? DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
    public string? LastUpdateSystem { get; init; }
    
    public string? CoreEntityChecksum { get; init; }
    
    public CoreStorageMeta ToBase() => new(
      new (OriginalSystem ?? throw new ArgumentNullException(nameof(OriginalSystem))),
      new (OriginalSystemId ?? throw new ArgumentNullException(nameof(OriginalSystemId))),
      new (CoreEntityTypeName ?? throw new ArgumentNullException(nameof(CoreEntityTypeName))),
      new (CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
      new (CoreEntityChecksum ?? throw new ArgumentNullException(nameof(CoreEntityChecksum))),
      DateCreated ?? throw new ArgumentNullException(nameof(DateCreated)),
      DateUpdated ?? throw new ArgumentNullException(nameof(DateUpdated)),
      new (LastUpdateSystem ?? throw new ArgumentNullException(nameof(LastUpdateSystem)))
    );
  }
}

public record CoreEntityAndMetaDtos(object CoreEntityDto, CoreStorageMeta.Dto MetaDto);
public record CoreEntityAndMetaDtos<D>(D CoreEntityDto, CoreStorageMeta.Dto MetaDto);

public record CoreEntityAndMeta(ICoreEntity CoreEntity, CoreStorageMeta Meta) { 
  public E As<E>() => (E) CoreEntity;
  public static CoreEntityAndMeta Create(SystemName system, SystemEntityId sysentid, ICoreEntity coreent, CoreEntityChecksum checksum) => 
      new(coreent, new CoreStorageMeta(system, sysentid, new(coreent.GetType().Name), coreent.CoreId, checksum, UtcDate.UtcNow, UtcDate.UtcNow, system));
  
  public static CoreEntityAndMeta Create(SystemName system, SystemEntityId sysentid, ICoreEntity coreent, Func<ICoreEntity, CoreEntityChecksum> checksumalg) => 
      Create(system, sysentid, coreent, checksumalg(coreent));

  public CoreEntityAndMeta Update(SystemName system, ICoreEntity coreent, CoreEntityChecksum checksum) => 
      new(coreent, Meta with { DateUpdated = UtcDate.UtcNow, LastUpdateSystem = system, CoreEntityChecksum = checksum });
  
  public CoreEntityAndMeta Update(SystemName system, ICoreEntity coreent, Func<ICoreEntity, CoreEntityChecksum> checksumalg) =>
      Update(system, coreent, checksumalg(coreent));
  
  public CoreEntityAndMetaDtos ToDtos() => new(DtoHelpers.ToDto(CoreEntity), DtoHelpers.ToDto<CoreStorageMeta, CoreStorageMeta.Dto>(Meta));
  
  public static CoreEntityAndMeta FromJson<E, D>(string json) where E : ICoreEntity where D : class, ICoreEntityDto<E> {
    var raw = Json.Deserialize<CoreEntityAndMetaDtos<D>>(json);
    return new CoreEntityAndMeta(raw.CoreEntityDto.ToBase(), raw.MetaDto.ToBase());
  }
}

public interface ICoreStorage : IAsyncDisposable {

  /// <summary>
  /// Gets all core entities that have been created/updated after the given `after` parameter.
  /// Also exclude all entities where `LastUpdateSystem` is `exclude`.  This prevents
  /// systems writing back their own changes.
  /// </summary>
  Task<List<CoreEntityAndMeta>> GetEntitiesToWrite([IgnoreNamingConventions] SystemName exclude, CoreEntityTypeName coretype, DateTime after);
  
  /// <summary>
  /// Gets all core entities of the specified type with the given Ids 
  /// </summary>
  Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  
  /// <summary>
  /// Gets the existing checksums of the specified entities that are already in core storage.
  /// These checksums are used to ignore unnecessary updates that could trigger unnecessary
  /// writes to other systems.
  /// 
  /// Note: If an entity is not in core storage, then it can be omitted from the returned dictionary.
  /// </summary>
  /// <returns>An id to checksum mapping of entities already in core storage</returns>
  Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  
  /// <summary>
  /// Upsert all entities into core storage
  /// </summary>
  Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities);
}