using System.Text.Json.Serialization;
using Centazio.Core.Checksum;

namespace Centazio.Core.Write;

public interface ISystemEntity : IGetChecksumSubset {

  public string SystemId { get; }
  
  [JsonIgnore] public string DisplayName { get; }
  
  public E To<E>() where E : ISystemEntity => (E) this;
}