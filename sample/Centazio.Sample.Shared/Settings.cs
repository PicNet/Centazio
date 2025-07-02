using Centazio.Core.Settings;

namespace Centazio.Sample.Shared;

public record Settings : CentazioSettings {

  public required CustomSettingSettings CustomSetting { get; init; }
  public required AppSheetSettings AppSheet { get; init; }
  
  protected Settings(CentazioSettings centazio) : base (centazio) {}
}

public record CustomSettingSettings {
  public string BaseUrl { get; }
  public string ListId { get; }
  
  private CustomSettingSettings(string baseurl, string listid) {
    BaseUrl = baseurl;
    ListId = listid;
  }
}

public record AppSheetSettings {
  public string BaseUrl { get; }
  public string AppId { get; }
  public string TableName { get; }
  
  private AppSheetSettings(string baseurl, string appid, string tablename) {
    BaseUrl = baseurl;
    AppId = appid;
    TableName = tablename;
  }
}