using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Sample;

public record SampleSettings : CentazioSettings {

  public ClickUpSettings ClickUp { get; }
  public AppSheetSettings AppSheet { get; }
  
  
  protected SampleSettings(CentazioSettings centazio, ClickUpSettings clickup, AppSheetSettings appsheet) : base(centazio) {
    ClickUp = clickup;
    AppSheet = appsheet;
  }
  
  public new record Dto : CentazioSettings.Dto, IDto<SampleSettings> {
    public ClickUpSettings.Dto? ClickUp { get; init; }
    public AppSheetSettings.Dto? AppSheet { get; init; }
    
    public new SampleSettings ToBase() => new(
        base.ToBase(), 
        ClickUp?.ToBase() ?? throw new SettingsSectionMissingException(nameof(ClickUp)), 
        AppSheet?.ToBase() ?? throw new SettingsSectionMissingException(nameof(AppSheet)));
  }
}

public record ClickUpSettings {
  public string BaseUrl { get; }
  public string ListId { get; }
  
  private ClickUpSettings(string baseurl, string listid) {
    BaseUrl = baseurl;
    ListId = listid;
  } 
  
  public record Dto : IDto<ClickUpSettings> {
    public string? BaseUrl { get; init; }
    public string? ListId { get; init; }
    
    public ClickUpSettings ToBase() => new (
      String.IsNullOrWhiteSpace(BaseUrl) ? throw new ArgumentNullException(nameof(BaseUrl)) : BaseUrl.Trim(),
      String.IsNullOrWhiteSpace(ListId) ? throw new ArgumentNullException(nameof(ListId)) : ListId.Trim()
    );
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
  
  public record Dto : IDto<AppSheetSettings> {
    public string? BaseUrl { get; init; }
    public string? AppId { get; init; }
    public string? TableName { get; init; }
    
    public AppSheetSettings ToBase() => new (
      String.IsNullOrWhiteSpace(BaseUrl) ? throw new ArgumentNullException(nameof(BaseUrl)) : BaseUrl.Trim(),
      String.IsNullOrWhiteSpace(AppId) ? throw new ArgumentNullException(nameof(AppId)) : AppId.Trim(),
      String.IsNullOrWhiteSpace(TableName) ? throw new ArgumentNullException(nameof(TableName)) : TableName.Trim()
    );
  }
}