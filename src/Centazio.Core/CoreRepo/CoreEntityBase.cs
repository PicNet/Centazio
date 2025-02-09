using System.Text.Json.Serialization;
using Centazio.Core.Misc;

namespace Centazio.Core.CoreRepo;

public abstract record CoreEntityBase(CoreEntityId CoreId) : ICoreEntity {

  [JsonIgnore] public abstract string DisplayName { get; }
  
  public CoreEntityId CoreId { get; set; } = CoreId;
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase() : this(CoreEntityId.DEFAULT_VALUE) { }

  public abstract record Dto<E> : ICoreEntityDto<E> where E : CoreEntityBase {
    public string CoreId { get; init; } = null!;
    
    public abstract E ToBase();
    
    protected E FillBaseProperties(E e) { 
      e.CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId)));
      return e;
    }
  }
}