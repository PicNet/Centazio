using System.Text.Json.Serialization;

namespace Centazio.Core.CoreRepo;

public interface ICoreEntity : IGetChecksumSubset {
  
  public CoreEntityId CoreId { get; set; }
  [JsonIgnore] public string DisplayName { get; }
    
  public E To<E>() where E : ICoreEntity => (E) this;
}
