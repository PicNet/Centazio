namespace Centazio.Core.CoreRepo;

public interface ICoreEntity {
  
  /// <summary>
  /// The source system where this entity was originally created
  /// </summary>
  public string SourceSystem { get; }
  
  /// <summary>
  /// The id of the entity.  Ideally this should be the same id as used in the source system.
  /// If this is not possible then this should be a short string representation of a unique
  /// identifier that can be used to map back to the source system 
  /// </summary>
  public string Id { get; }
  
  /// <summary>
  /// A checksum used to check for unnecessary updates to already existing entities in
  /// core storage.  If this is empty then checksum comparisons will not be made and all
  /// updates from the source system will be replicated to core storage even if nothing
  /// meaningful has changed.
  /// </summary>
  public string Checksum { get; }
  
  /// <summary>
  /// The date/time when this entity was added to core storage
  /// </summary>
  public DateTime DateCreated { get; }
  
  /// <summary>
  /// The date/time when this entity was last updated in core storage
  /// </summary>
  public DateTime DateUpdated { get; }
  
  /// <summary>
  /// The date/time when this entity was last updated in the source system.  This requires that the source system
  /// provides this 'updated' date.  If the source system does not provide an updated date field then this can
  /// be set to `DateUpdated`
  /// </summary>
  public DateTime SourceSystemDateUpdated { get; }
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
  Task<Dictionary<string, string>> GetChecksums<T>(List<T> entities) where T : ICoreEntity;
  
  /// <summary>
  /// Upsert all entities into core storage
  /// </summary>
  Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity;
}
