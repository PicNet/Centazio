using Centazio.Core.Checksum;
using Centazio.Core.Misc;

namespace Centazio.Core.CoreRepo;

public interface ICoreStorageGetter : IAsyncDisposable {

  /// <summary>
  /// Gets all core entities that have been created/updated after the given `after` parameter.
  /// Also exclude all entities where `LastUpdateSystem` is `exclude`.  This prevents
  /// systems writing back their own changes.
  /// </summary>
  Task<List<ICoreEntity>> Get([IgnoreNamingConventions] SystemName exclude, CoreEntityTypeName coretype, DateTime after);
  
  /// <summary>
  /// Gets all core entities of the specified type with the given Ids 
  /// </summary>
  Task<List<ICoreEntity>> Get(CoreEntityTypeName coretype, List<CoreEntityId> coreids);
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
  Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  
  /// <summary>
  /// Upsert all entities into core storage
  /// </summary>
  Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<Containers.CoreChecksum> entities);
}

public interface ICoreStorage : ICoreStorageGetter, ICoreStorageUpserter;