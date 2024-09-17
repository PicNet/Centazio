namespace Centazio.Core.CoreRepo;

public interface ICoreStorageUpserter : IAsyncDisposable {
  
  /// <summary>
  /// Gets the existing checksums of the specified entities that are already in core storage.
  /// These checksums are used to ignore unnecessary updates that could trigger unnecessary
  /// writes to other systems.
  /// 
  /// Note: If an entity is not in core storage, then it can be omitted from the returned dictionary.
  /// </summary>
  /// <returns>An id to checksim mapping of entities already in core storage</returns>
  Task<Dictionary<string, string>> GetChecksums<T>(List<T> entities) where T : ICoreEntity;
  
  /// <summary>
  /// Upsert all entities into core storage
  /// </summary>
  Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity;
}
