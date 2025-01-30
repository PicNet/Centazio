using System.Text.Json.Serialization;

namespace Centazio.Core.Types;

/// <summary>
/// A checksum used to check for unnecessary writes to core storage or the target system.
/// If this returns null then checksum comparisons will not be made and all updates
/// written even if nothing meaningful has changed.
///
/// Implementing methods should return a subset of the entity fields that
/// signify meaningful changes.
///
/// This means ignoring redundant Ids, children collections, date updated, etc.  However,
/// this object should always include the main primary id of the object.
///
/// Example:
/// ```
/// public object GetChecksumSubset() => new { Id, Name, Address };
/// ```
/// </summary>
public interface IGetChecksumSubset {
  object GetChecksumSubset();
}

public interface IHasDisplayName {
  
  [JsonIgnore] public string DisplayName { get; }
  string GetId();
  
  public string GetShortDisplayName() {
    var maxlen = 100;
    var shortnm = DisplayName.Length > maxlen ? DisplayName[..maxlen] + "..." : DisplayName;
    return $"{shortnm}({GetId()})";
  }

}

public interface ICoreEntity : IHasDisplayName, IGetChecksumSubset {
  
  public CoreEntityId CoreId { get; set; }
    
  string IHasDisplayName.GetId() => CoreId.Value;
  public E To<E>() where E : ICoreEntity => (E) this;
}


public interface ISystemEntity : IHasDisplayName, IGetChecksumSubset {

  [JsonIgnore] public SystemEntityId SystemId { get; }
  [JsonIgnore] public DateTime LastUpdatedDate { get; }
  
  string IHasDisplayName.GetId() => SystemId.Value;
  public E To<E>() where E : ISystemEntity => (E) this;
  
  ISystemEntity CreatedWithId(SystemEntityId newid);

}

public interface IServiceFactory<out S> {
  S GetService();
}