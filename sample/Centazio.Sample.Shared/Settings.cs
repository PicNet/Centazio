using Centazio.Core.Settings;

namespace Centazio.Sample.Shared;

public record Settings : CentazioSettings {

  public required ClickUpSettings ClickUp { get; init; }
  public required AppSheetSettings AppSheet { get; init; }
  
  public Settings() : this(new CentazioSettings()) {} // for json serialisation
  protected Settings(CentazioSettings centazio) : base (centazio) {}
}

public record ClickUpSettings(string BaseUrl, string ListId) {
  public ClickUpSettings() : this(null!, null!) {}
}
public record AppSheetSettings(string BaseUrl, string AppId, string TableName) {
  public AppSheetSettings() : this(null!, null!, null!) {}
}