using System.Text.Json.Serialization;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Core.CoreRepo;

public interface ICoreEntity : IGetChecksumSubset {
  
  public CoreEntityId CoreId { get; set; }
  [JsonIgnore] public string DisplayName { get; }
    
  public E To<E>() where E : ICoreEntity => (E) this;
}


public abstract record CoreEntityBase(CoreEntityId CoreId) : ICoreEntity {

  public CoreEntityId CoreId { get; set; } = CoreId;
  [JsonIgnore] public abstract string DisplayName { get; }
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase() : this((CoreEntityId) null!) { }

  public abstract record Dto<E> : ICoreEntityDto<E> where E : CoreEntityBase {
    public string CoreId { get; init; } = null!;
    
    public abstract E ToBase();
    
    protected E FillBaseProperties(E e) { 
      e.CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId)));
      return e;
    }
  }
}