using System.Text.Json.Serialization;

namespace Centazio.Core.CoreRepo;

public abstract record CoreEntityBase(CoreEntityId CoreId, CorrelationId CorrelationId) : ICoreEntity {

  [JsonIgnore] public abstract string DisplayName { get; }
  
  public CoreEntityId CoreId { get; set; } = CoreId;
  public CorrelationId CorrelationId { get; set; } = CorrelationId;
  
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase() : this(CoreEntityId.DEFAULT_VALUE, CorrelationId.DEFAULT_VALUE) { }

  public abstract record Dto<E> : ICoreEntityDto<E> where E : CoreEntityBase {
    public required string CoreId { get; init; }
    public required string CorrelationId { get; init; } 
    
    public abstract E ToBase();
  }
}