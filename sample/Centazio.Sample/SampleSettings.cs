using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Sample;

public record SampleSettings : CentazioSettings {

  public required ClickUpSettings ClickUp { get; init; }
  public required AppSheetSettings AppSheet { get; init; }
  
  protected SampleSettings(CentazioSettings centazio) : base (centazio) {}

  public override Dto ToDto() {
    return new(base.ToDto()) {
      ClickUp = ClickUp.ToDto(),
      AppSheet = AppSheet.ToDto(),
    };
  }

  public new record Dto : CentazioSettings.Dto, IDto<SampleSettings> {
    public ClickUpSettings.Dto? ClickUp { get; init; }
    public AppSheetSettings.Dto? AppSheet { get; init; }
    
    public Dto() {} // required for initialisation in `SettingsLoader.cs`
    internal Dto(CentazioSettings.Dto centazio) : base(centazio) {}
    
    public new SampleSettings ToBase() {
      var centazio = base.ToBase();
      return new SampleSettings(centazio) {
        // compiler does not know that `base.ToBase()` has already set `SecretsFolders`
        SecretsFolders = centazio.SecretsFolders,  
        ClickUp = ClickUp?.ToBase() ?? throw new SettingsSectionMissingException(nameof(ClickUp)),
        AppSheet = AppSheet?.ToBase() ?? throw new SettingsSectionMissingException(nameof(AppSheet)) 
      };
    }

  }
}

public record ClickUpSettings {
  public string BaseUrl { get; }
  public string ListId { get; }
  
  private ClickUpSettings(string baseurl, string listid) {
    BaseUrl = baseurl;
    ListId = listid;
  }

  public Dto ToDto() => new() {
    BaseUrl = String.IsNullOrWhiteSpace(BaseUrl) ? throw new ArgumentNullException(nameof(BaseUrl)) : BaseUrl.Trim(),
    ListId = String.IsNullOrWhiteSpace(ListId) ? throw new ArgumentNullException(nameof(ListId)) : ListId.Trim()
  };

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

  public Dto ToDto() => new() {
    BaseUrl = String.IsNullOrWhiteSpace(BaseUrl) ? throw new ArgumentNullException(nameof(BaseUrl)) : BaseUrl.Trim(),
    AppId = String.IsNullOrWhiteSpace(AppId) ? throw new ArgumentNullException(nameof(AppId)) : AppId.Trim(),
    TableName = String.IsNullOrWhiteSpace(TableName) ? throw new ArgumentNullException(nameof(TableName)) : TableName.Trim()
  };

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