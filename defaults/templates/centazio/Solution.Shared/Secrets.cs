using Centazio.Core.Secrets;

namespace {{ it.Namespace }};

public record Secrets : CentazioSecrets {

  public string ADDITIONAL_SAMPLE_SECRET { get; }
  
  public Secrets(CentazioSecrets centazio, string ADDITIONAL_SAMPLE_SECRET) : base(centazio) {
    this.ADDITIONAL_SAMPLE_SECRET = ADDITIONAL_SAMPLE_SECRET;
  }

  public new record Dto :  CentazioSecrets.Dto, IDto<Secrets> {
    public string? ADDITIONAL_SAMPLE_SECRET { get; init; }
    
    public new Secrets ToBase() => new(base.ToBase(), 
        String.IsNullOrWhiteSpace(ADDITIONAL_SAMPLE_SECRET) ? throw new ArgumentNullException(nameof(ADDITIONAL_SAMPLE_SECRET)) : ADDITIONAL_SAMPLE_SECRET.Trim());
  }
}