using System.ComponentModel.DataAnnotations;

namespace {{ it.Namespace }};

public static class CoreEntityTypes {
  public static readonly CoreEntityTypeName ExampleEntity = new(nameof(ExampleEntity));
}

public record ExampleEntity : CoreEntityBase {
  [MaxLength(128)] public string Name { get; init; }
  public bool Completed { get; set; }
  public override string DisplayName => Name;
  
  private ExampleEntity() { Name = String.Empty; }
  
  public ExampleEntity(CoreEntityId coreid, CorrelationId corrid, string name, bool completed) : base(coreid, corrid) {
    Name = name;
    Completed = completed;
  }

  public override object GetChecksumSubset() => new { CoreId, Name, Completed };
  
  public record Dto : Dto<ExampleEntity> {
    public string? Name { get; init; }
    public bool Completed { get; init; }
    
    public override ExampleEntity ToBase() => new() {
      CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
      Name = String.IsNullOrWhiteSpace(Name) ? throw new ArgumentNullException(nameof(Name)) : Name.Trim(),
      Completed = Completed
    };
  }
}