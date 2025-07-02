using System.Text.Json.Serialization;

namespace Centazio.Core.CoreRepo;

public abstract record CoreEntityBase(CoreEntityId CoreId) : ICoreEntity {

  [JsonIgnore] public abstract string DisplayName { get; }
  
  public CoreEntityId CoreId { get; set; } = CoreId;
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase() : this(CoreEntityId.DEFAULT_VALUE) { }
}