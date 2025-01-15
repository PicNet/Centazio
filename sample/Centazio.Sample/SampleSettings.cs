using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Sample;

public record SampleSettings : CentazioSettings {

  private readonly ClickUpSettings? _ClickUp;
  public ClickUpSettings ClickUp => _ClickUp ?? throw new Exception($"ClickUp section missing from SampleSettings");
  
  private readonly GoogleSheetsSettings? _GoogleSheets;
  public GoogleSheetsSettings GoogleSheets => _GoogleSheets ?? throw new Exception($"GoogleSheets section missing from SampleSettings");
  
  protected SampleSettings(CentazioSettings centazio, ClickUpSettings? clickup, GoogleSheetsSettings? googlesheets) : base(centazio) {
    _ClickUp = clickup;
    _GoogleSheets = googlesheets;
  }
  
  public new record Dto : CentazioSettings.Dto, IDto<SampleSettings> {
    public ClickUpSettings.Dto? ClickUp { get; init; }
    public GoogleSheetsSettings.Dto? GoogleSheets { get; init; }
    
    public new SampleSettings ToBase() => new(base.ToBase(), ClickUp?.ToBase(), GoogleSheets?.ToBase());
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

public record GoogleSheetsSettings {
  public string CredentialsFile { get; }
  public string SheetId { get; }
  
  private GoogleSheetsSettings(string credentialsfile, string sheetid) {
    CredentialsFile = credentialsfile;
    SheetId = sheetid;
  } 
  
  public record Dto : IDto<GoogleSheetsSettings> {
    public string? CredentialsFile { get; init; }
    public string? SheetId { get; init; }
    
    public GoogleSheetsSettings ToBase() => new (
      String.IsNullOrWhiteSpace(CredentialsFile) ? throw new ArgumentNullException(nameof(CredentialsFile)) : CredentialsFile.Trim(),
      String.IsNullOrWhiteSpace(SheetId) ? throw new ArgumentNullException(nameof(SheetId)) : SheetId.Trim()
    );
  }
}