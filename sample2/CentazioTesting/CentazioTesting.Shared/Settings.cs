using Centazio.Core.Settings;

namespace CentazioTesting.Shared;

public record Settings : CentazioSettings {

  public required CustomSettingSettings CustomSetting { get; init; }
  
  protected Settings(CentazioSettings centazio) : base (centazio) {}

  public override Dto ToDto() {
    return new(base.ToDto()) { CustomSetting = CustomSetting.ToDto() };
  }

  public new record Dto : CentazioSettings.Dto, IDto<Settings> {
    public CustomSettingSettings.Dto? CustomSetting { get; init; }
    
    public Dto() {} // required for initialisation in `SettingsLoader.cs`
    internal Dto(CentazioSettings.Dto centazio) : base(centazio) {}
    
    public new Settings ToBase() {
      var centazio = base.ToBase();
      return new Settings(centazio) {
        // compiler does not know that `base.ToBase()` has already set `SecretsFolders`
        SecretsFolders = centazio.SecretsFolders,  
        CustomSetting = CustomSetting?.ToBase() ?? throw new SettingsSectionMissingException(nameof(CustomSetting)) 
      };
    }

  }
}

public record CustomSettingSettings {
  public string ExampleProperty { get; }
  
  private CustomSettingSettings(string propval) {
    ExampleProperty = propval;
  }

  public Dto ToDto() => new() {
    ExampleProperty = String.IsNullOrWhiteSpace(ExampleProperty) ? throw new ArgumentNullException(nameof(ExampleProperty)) : ExampleProperty.Trim()
  };

  public record Dto : IDto<CustomSettingSettings> {
    public string? ExampleProperty { get; init; }
    
    public CustomSettingSettings ToBase() => new (
      String.IsNullOrWhiteSpace(ExampleProperty) ? throw new ArgumentNullException(nameof(ExampleProperty)) : ExampleProperty.Trim()
    );
  }
}
