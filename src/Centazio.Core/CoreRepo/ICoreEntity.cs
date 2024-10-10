using System.Text.Json.Serialization;
using Centazio.Core.Misc;

namespace Centazio.Core.CoreRepo;

public interface ICoreEntity : IGetChecksumSubset {
  
  /// <summary>
  /// The source system where this entity was originally created
  /// </summary>
  public SystemName System { get; }
  
  /// <summary>
  /// The id of the entity in the source system 
  /// </summary>
  public SystemEntityId SystemId { get; set; }

  /// <summary>
  /// The id of the entity.  Ideally this should be the same id as used in the source
  /// system (i.e. same as `SourceId`). If this is not possible then this should be a
  /// short string representation of a unique identifier that can be used to map back
  /// to the source system 
  /// </summary>
  public CoreEntityId CoreId { get; set; }
  
  /// <summary>
  /// The date/time when this entity was added to core storage
  /// </summary>
  public DateTime DateCreated { get; set; }

  /// <summary>
  /// The date/time when this entity was last updated in core storage
  /// </summary>
  public DateTime DateUpdated { get; set; }
  
  /// <summary>
  /// The system that triggered the last update.  This is used to
  /// filter out any changes by a system, when writing back to that system.  I.e. if
  /// System 1 changes a property, then there is no need to write this back to System 1.
  /// </summary>
  [IgnoreNamingConventions] public SystemName LastUpdateSystem { get; set; }

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
