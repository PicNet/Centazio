namespace Centazio.Core.CoreRepo;

public interface ICoreEntity {
  
  /// <summary>
  /// The source system where this entity was originally created
  /// </summary>
  public string SourceSystem { get; }
  
  /// <summary>
  /// The id of the entity in the source system 
  /// </summary>
  public string SourceId { get; }
  
  /// <summary>
  /// The id of the entity.  Ideally this should be the same id as used in the source
  /// system (i.e. same as `SourceId`). If this is not possible then this should be a
  /// short string representation of a unique identifier that can be used to map back
  /// to the source system 
  /// </summary>
  public string Id { get; }
  
  /// <summary>
  /// A checksum used to check for unnecessary updates to already existing entities in
  /// core storage.  If this is empty then checksum comparisons will not be made and all
  /// updates from the source system will be replicated to core storage even if nothing
  /// meaningful has changed
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
  
  public T To<T>() where T : ICoreEntity => (T) this;
}
