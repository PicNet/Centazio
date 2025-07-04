using Centazio.Core.Secrets;

namespace Centazio.Sample.Shared;

public record Secrets : CentazioSecrets {

  public string CLICKUP_TOKEN { get; set; }
  public string APPSHEET_KEY { get; set; }
  
  public Secrets() : this(new CentazioSecrets(), null!, null!) {} // for Json serialisation
  public Secrets(CentazioSecrets centazio, string CLICKUP_TOKEN, string APPSHEET_KEY) : base(centazio) {
    this.CLICKUP_TOKEN = CLICKUP_TOKEN;
    this.APPSHEET_KEY = APPSHEET_KEY;
  }
}