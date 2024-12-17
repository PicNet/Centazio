using Centazio.Core;
using Centazio.Core.Settings;

namespace Centazio.Sample;

// todo: this is not great, consider using `newjson` branch
//    code for automatic json deserialisation for settings and secrets?
public record SampleSettings : CentazioSettings {

  private readonly ClickUpSettings? _ClickUp;
  public ClickUpSettings ClickUp => _ClickUp ?? throw new Exception($"ClickUp section missing from SampleSettings");
  
  protected SampleSettings(CentazioSettings centazio, ClickUpSettings? clickup) : base(centazio) {
    _ClickUp = clickup;
  }
  
  public new record Dto : CentazioSettings.Dto, IDto<SampleSettings> {
    public ClickUpSettings.Dto? ClickUp { get; init; }
    
    public new SampleSettings ToBase() => new(base.ToBase(), ClickUp?.ToBase());

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