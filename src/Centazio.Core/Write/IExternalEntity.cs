using System.Text.Json.Serialization;

namespace Centazio.Core.Write;

// todo: if we are just using this for Writes then perhaps imply that in the name
//    currently it looks like we could also use this when promoting (converting
//    staged string -> IExternalEntity)
public interface IExternalEntity {

  // todo: Id is not a great name as its too common and causes conflicts, rename to something a bit better 
  public string Id { get; }
  
  [JsonIgnore] public string DisplayName { get; }
  
  /// <summary>
  /// A checksum used to check for unnecessary writes to the target system. If this returns null
  /// then checksum comparisons will not be made and all updates written even if nothing
  /// meaningful has changed.
  ///
  /// Implementing methods should return a subset of the entity fields that
  /// signify meaningful changes.
  ///
  /// Example:
  /// ```
  /// public object? GetChecksumSubset() => new { Name, Address };
  /// ```
  /// </summary>
  public object? GetChecksumSubset();
  
  public E To<E>() where E : IExternalEntity => (E) this;

}