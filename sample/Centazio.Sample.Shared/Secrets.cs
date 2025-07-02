using Centazio.Core.Secrets;

namespace Centazio.Sample.Shared;

public record Secrets : CentazioSecrets {

  public string CLICKUP_TOKEN { get; }
  public string APPSHEET_KEY { get; }
  
  public Secrets(CentazioSecrets centazio, string CLICKUP_TOKEN, string APPSHEET_KEY) : base(centazio) {
    this.CLICKUP_TOKEN = CLICKUP_TOKEN;
    this.APPSHEET_KEY = APPSHEET_KEY;
  }
}