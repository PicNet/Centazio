using Centazio.Core.Secrets;

namespace Centazio.Sample;

public record SampleSecrets : CentazioSecrets {

  public string CLICKUP_TOKEN { get; }
  public string APPSHEET_KEY { get; }
  
  public SampleSecrets(CentazioSecrets centazio, string CLICKUP_TOKEN, string APPSHEET_KEY) : base(centazio) {
    this.CLICKUP_TOKEN = CLICKUP_TOKEN;
    this.APPSHEET_KEY = APPSHEET_KEY;
  }

  public new record Dto :  CentazioSecrets.Dto, IDto<SampleSecrets> {
    public string? CLICKUP_TOKEN { get; init; }
    public string? APPSHEET_KEY { get; init; }
    
    public new SampleSecrets ToBase() => 
        new(base.ToBase(), 
        String.IsNullOrWhiteSpace(CLICKUP_TOKEN) ? throw new ArgumentNullException(nameof(CLICKUP_TOKEN)) : CLICKUP_TOKEN.Trim(),
        String.IsNullOrWhiteSpace(APPSHEET_KEY) ? throw new ArgumentNullException(nameof(APPSHEET_KEY)) : APPSHEET_KEY.Trim());

  }
}