using System.Text.Json.Serialization;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Core.CoreRepo;

public interface ICoreEntity : IGetChecksumSubset {
  
  public CoreEntityId CoreId { get; set; }
  [JsonIgnore] public string DisplayName { get; }
    
  public E To<E>() where E : ICoreEntity => (E) this;
}


public abstract record CoreEntityBase : ICoreEntity {
  public CoreEntityId CoreId { get; set; }
  
  [JsonIgnore] public abstract string DisplayName { get; }
  public abstract object GetChecksumSubset();
  
  // todo: clean this up
  protected CoreEntityBase() { CoreId = null!; }
  protected CoreEntityBase(CoreEntityId coreid) => CoreId = coreid;

  public abstract record Dto<E> : ICoreEntityDto<E> 
      where E : CoreEntityBase {
    public string CoreId { get; init; } = null!;
    
    public abstract E ToBase();
    
    protected E FillBaseProperties(E e) { 
      e.CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId)));
      return e;
    }
  }
}