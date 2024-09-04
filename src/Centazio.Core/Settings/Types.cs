namespace Centazio.Core.Settings;

public record AzureFunctionSettingsRaw {
    public string? FunctionAppId { get; init; }
    public string? FunctionClassName { get; init; }
    
    public static explicit operator AzureFunctionSettings(AzureFunctionSettingsRaw raw) => new (
        raw.FunctionAppId ?? throw new ArgumentNullException(nameof(FunctionAppId)),
        raw.FunctionClassName ?? throw new ArgumentNullException(nameof(FunctionClassName)));
}

public record AzureSettingsRaw {
    public string? Region { get; init; }
    public string? ResourceGroup { get; init; }
    public string? FunctionStorageAccId { get; init; }
    public string? AppServicePlanId { get; init; }
    public List<AzureFunctionSettingsRaw>? Functions { get; init; }
    
    public static explicit operator AzureSettings(AzureSettingsRaw raw) => new (
          raw.Region ?? throw new ArgumentNullException(nameof(Region)),
          raw.ResourceGroup ?? throw new ArgumentNullException(nameof(ResourceGroup)),
          raw.FunctionStorageAccId ?? throw new ArgumentNullException(nameof(FunctionStorageAccId)),
          raw.AppServicePlanId ?? throw new ArgumentNullException(nameof(AppServicePlanId)),
          raw.Functions?.Select(f => (AzureFunctionSettings) f).ToList());
}

public record AwsSettingsRaw {
    public string? AccountName { get; init; }
    
    public static explicit operator AwsSettings(AwsSettingsRaw raw) => new (
          raw.AccountName ?? throw new ArgumentNullException(nameof(AccountName)));
}

public record CentazioSettingsRaw {
  public string? SecretsFolder { get; init; }
  public AwsSettingsRaw? AwsSettings { get; init; }
  public AzureSettingsRaw? AzureSettings { get; init; }
  
  public static explicit operator CentazioSettings(CentazioSettingsRaw raw) => new (
          raw.SecretsFolder ?? throw new ArgumentNullException(nameof(SecretsFolder)),
          raw.AwsSettings == null ? null : (AwsSettings) raw.AwsSettings,
          raw.AzureSettings == null ? null : (AzureSettings) raw.AzureSettings);
}

public record AzureFunctionSettings(string FunctionAppId, string FunctionClassName);

public record AzureSettings(string Region, string ResourceGroup, string FunctionStorageAccId, string AppServicePlanId, List<AzureFunctionSettings>? Functions);

public record AwsSettings(string AccountName);

public record CentazioSettings(string SecretsFolder, AwsSettings? AwsSettings, AzureSettings? AzureSettings);
