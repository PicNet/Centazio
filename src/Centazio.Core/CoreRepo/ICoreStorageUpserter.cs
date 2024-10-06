using Centazio.Core.Checksum;

namespace Centazio.Core.CoreRepo;

public interface ICoreStorageGetter : IAsyncDisposable {

  /// <summary>
  /// Gets all core entities that have been created/updated after the given `after` parameter.
  /// Also exclude all entities where `LastUpdateSystem` is `exclude`.  This prevents
  /// systems writing back their own changes.
  /// </summary>
  Task<List<ICoreEntity>> Get(CoreEntityType obj, DateTime after, SystemName exclude);
  
  /// <summary>
  /// Gets all core entities of the specified type with the given Ids 
  /// </summary>
  Task<List<ICoreEntity>> Get(CoreEntityType obj, List<string> coreids);
}

public interface ICoreStorageUpserter : IAsyncDisposable {
  
  /// <summary>
  /// Gets the existing checksums of the specified entities that are already in core storage.
  /// These checksums are used to ignore unnecessary updates that could trigger unnecessary
  /// writes to other systems.
  /// 
  /// Note: If an entity is not in core storage, then it can be omitted from the returned dictionary.
  /// </summary>
  /// <returns>An id to checksum mapping of entities already in core storage</returns>
  Task<Dictionary<string, CoreEntityChecksum>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities);
  
  /// <summary>
  /// Upsert all entities into core storage
  /// </summary>
  Task<List<ICoreEntity>> Upsert(CoreEntityType obj, List<Containers.CoreChecksum> entities);
}

public interface ICoreStorage : ICoreStorageGetter, ICoreStorageUpserter;