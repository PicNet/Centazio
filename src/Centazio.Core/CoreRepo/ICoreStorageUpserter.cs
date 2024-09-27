namespace Centazio.Core.CoreRepo;

public interface ICoreStorageGetter : IAsyncDisposable {

  /// <summary>
  /// Gets all core entities that have been created/updated after the given `after` parameter.
  /// </summary>
  Task<List<ICoreEntity>> Get(ObjectName obj, DateTime after);
}

public interface ICoreStorageUpserter : IAsyncDisposable {
  
  /// <summary>
  /// Gets the existing checksums of the specified entities that are already in core storage.
  /// These checksums are used to ignore unnecessary updates that could trigger unnecessary
  /// writes to other systems.
  /// 
  /// Note: If an entity is not in core storage, then it can be omitted from the returned dictionary.
  /// </summary>
  /// <returns>An id to checksim mapping of entities already in core storage</returns>
  Task<Dictionary<string, string>> GetChecksums(ObjectName obj, List<ICoreEntity> entities);
  
  /// <summary>
  /// Upsert all entities into core storage
  /// </summary>
  Task<IEnumerable<ICoreEntity>> Upsert(ObjectName obj, IEnumerable<ICoreEntity> entities);
}
