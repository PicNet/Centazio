using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;

namespace Centazio.Sample;

public record SampleSecrets : CentazioSecrets {

  public string CLICKUP_TOKEN { get; }
  
  public SampleSecrets(CentazioSecrets centazio, string CLICKUP_TOKEN) : base(centazio) {
    this.CLICKUP_TOKEN = CLICKUP_TOKEN;
  }

  public new record Dto :  CentazioSecrets.Dto, IDto<SampleSecrets> {
    public string? CLICKUP_TOKEN { get; init; }
    
    public new SampleSecrets ToBase() => 
        new(base.ToBase(), 
        String.IsNullOrWhiteSpace(CLICKUP_TOKEN) ? throw new ArgumentNullException(nameof(CLICKUP_TOKEN)) : CLICKUP_TOKEN.Trim());

  }
}