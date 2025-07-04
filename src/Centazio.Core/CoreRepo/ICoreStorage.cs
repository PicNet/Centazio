using Centazio.Core.Checksum;

namespace Centazio.Core.CoreRepo;

public record CoreStorageMeta(
    SystemName OriginalSystem,
    SystemEntityTypeName OriginalSystemType,
    SystemEntityId OriginalSystemId, 
    CoreEntityTypeName CoreEntityTypeName, 
    CoreEntityId CoreId, 
    CoreEntityChecksum CoreEntityChecksum, 
    DateTime DateCreated, 
    DateTime DateUpdated, 
    SystemName LastUpdateSystem, 
    SystemEntityId LastUpdateSystemId) {
  
  private CoreStorageMeta() : this(null!, null!, null!, null!, null!, null!, DateTime.MinValue, DateTime.MinValue, null!, null!) { } // for serialisation
}

public record CoreEntityAndMeta(ICoreEntity CoreEntity, CoreStorageMeta Meta) { 
  public E As<E>() => (E) CoreEntity;
  public static CoreEntityAndMeta Create(SystemName system, SystemEntityTypeName systype, SystemEntityId sysentid, ICoreEntity coreent, CoreEntityChecksum checksum) => 
      new(coreent, new CoreStorageMeta(system, systype, sysentid, new(coreent.GetType().Name), coreent.CoreId, checksum, UtcDate.UtcNow, UtcDate.UtcNow, system, sysentid));
  
  public static CoreEntityAndMeta Create(SystemName system, SystemEntityTypeName systype, SystemEntityId sysentid, ICoreEntity coreent, Func<ICoreEntity, CoreEntityChecksum> checksumalg) => 
      Create(system, systype, sysentid, coreent, checksumalg(coreent));

  public CoreEntityAndMeta Update(SystemName system, ICoreEntity coreent, CoreEntityChecksum checksum) => 
      new(coreent, Meta with { DateUpdated = UtcDate.UtcNow, LastUpdateSystem = system, CoreEntityChecksum = checksum });
  
  public CoreEntityAndMeta Update(SystemName system, ICoreEntity coreent, Func<ICoreEntity, CoreEntityChecksum> checksumalg) =>
      Update(system, coreent, checksumalg(coreent));
  
  public static CoreEntityAndMeta FromJson(string json) => 
      Json.Deserialize<CoreEntityAndMeta>(json);

}

public interface ICoreStorage : IAsyncDisposable {

  Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null);
  
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