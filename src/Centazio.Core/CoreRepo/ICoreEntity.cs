using System.Text.Json.Serialization;

namespace Centazio.Core.CoreRepo;

public record CoreEntityAndChecksum {
  public ICoreEntity CoreEntity { get; private init; }
  public string Checksum { get; private init; }
  
  public CoreEntityAndChecksum(ICoreEntity core, Func<object, string> checksum) {
    var subset = core.GetChecksumSubset();
    
    CoreEntity = core;
    Checksum = subset is null ? String.Empty : checksum(subset);
  }
  
  public T ToCore<T>() where T : ICoreEntity => (T) CoreEntity; 
}

public interface ICoreEntity {
  
  /// <summary>
  /// The source system where this entity was originally created
  /// </summary>
  public string SourceSystem { get; }
  
  /// <summary>
  /// The id of the entity in the source system 
  /// </summary>
  public string SourceId { get; set; }

  /// <summary>
  /// The id of the entity.  Ideally this should be the same id as used in the source
  /// system (i.e. same as `SourceId`). If this is not possible then this should be a
  /// short string representation of a unique identifier that can be used to map back
  /// to the source system 
  /// </summary>
  public string Id { get; set; }

  /// <summary>
  /// A checksum used to check for unnecessary updates to already existing entities in
  /// core storage.  If this returns null then checksum comparisons will not be made and all
  /// updates from the source system will be replicated to core storage even if nothing
  /// meaningful has changed.
  ///
  /// Implementing methods should return a subset of the entity fields that
  /// signify meaningful changes.
  ///
  /// Example:
  /// ```
  /// public object? GetChecksumSubset() => new {
  ///   Name,
  ///   Address,
  ///   // Usually we do not include children entities as children have the foreign key field.
  ///   // This means that if a child changes, the parent does not really change and does not
  ///   // need to be promoted.
  ///   // Children = Children.Select(c => c.GetChecksumSubset()).ToList(),
  ///
  ///   // If the relationship to the parent changes, we must recognise this and promote again
  ///   Parent = Parent.GetChecksumSubset()
  /// };
  /// ```
  /// </summary>
  /// <param name="system">
  /// The current function's system.  This can be used to tailor the checksum for each system
  /// as some systems do not have todo: remove if not necessery
  /// </param>
  public object? GetChecksumSubset();
  
  /// <summary>
  /// The date/time when this entity was added to core storage
  /// </summary>
  public DateTime DateCreated { get; }
  
  /// <summary>
  /// The date/time when this entity was last updated in core storage
  /// </summary>
  public DateTime DateUpdated { get; }
  
  /// <summary>
  /// The system that triggered the last update.  This is used to
  /// filter out any changes by a system, when writing back to that system.  I.e. if
  /// System 1 changes a property, then there is no need to write this back to System 1.
  /// </summary>
  public string LastUpdateSystem { get; }
  
  /// <summary>
  /// The date/time when this entity was last updated in the source system.  This requires that the source system
  /// provides this 'updated' date.  If the source system does not provide an updated date field then this can
  /// be set to `DateUpdated`
  /// </summary>
  public DateTime SourceSystemDateUpdated { get; }
  
  /// <summary>
  /// A descriptive field that can be used for debugging and displaying the
  /// entity.  This can be any field in the real entity, and can even be
  /// blank if there is no logical name or descriptive field.
  ///
  /// Try to keep the contents of this field to a small size so it does not
  /// polute logs and displays.
  ///
  /// Note: This field is internal as its only used for internal logging and should
  /// not be saved in storage.
  /// </summary>
  [JsonIgnore] public string DisplayName { get; }
    
  public E To<E>() where E : ICoreEntity => (E) this;
}
