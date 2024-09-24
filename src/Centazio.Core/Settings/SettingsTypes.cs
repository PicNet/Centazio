namespace Centazio.Core.Settings;

public record AzureFunctionSettings {
  public string FunctionAppId { get; } 
  public string FunctionClassName { get; }
  
  private AzureFunctionSettings(string appid, string classname) {
    FunctionAppId = appid;
    FunctionClassName = classname;
  }
  
  public record Dto {
    public string? FunctionAppId { get; init; }
    public string? FunctionClassName { get; init; }
    
    public static explicit operator AzureFunctionSettings(Dto dto) => new (
        String.IsNullOrWhiteSpace(dto.FunctionAppId) ? throw new ArgumentNullException(nameof(FunctionAppId)) : dto.FunctionAppId.Trim(),
        String.IsNullOrWhiteSpace(dto.FunctionClassName) ? throw new ArgumentNullException(nameof(FunctionClassName)) : dto.FunctionClassName.Trim());
  }
}

public record AzureSettings {
  public string Region { get; }
  public string ResourceGroup { get; }
  public string FunctionStorageAccId { get; }
  public string AppServicePlanId { get; }
  public List<AzureFunctionSettings>? Functions { get; }
  
  private AzureSettings(string region, string resourcegroup, string storageacc, string svcplanid, List<AzureFunctionSettings>? functions) {
    Region = region;  
    ResourceGroup = resourcegroup;
    FunctionStorageAccId = storageacc;
    AppServicePlanId = svcplanid;
    Functions = functions;
  }
  
  
  public record Dto {
    public string? Region { get; init; }
    public string? ResourceGroup { get; init; }
    public string? FunctionStorageAccId { get; init; }
    public string? AppServicePlanId { get; init; }
    public List<AzureFunctionSettings.Dto>? Functions { get; init; }
    
    public static explicit operator AzureSettings(Dto dto) => new (
        String.IsNullOrWhiteSpace(dto.Region) ? throw new ArgumentNullException(nameof(Region)) : dto.Region.Trim(),
        String.IsNullOrWhiteSpace(dto.ResourceGroup) ? throw new ArgumentNullException(nameof(ResourceGroup)) : dto.ResourceGroup.Trim(),
        String.IsNullOrWhiteSpace(dto.FunctionStorageAccId) ? throw new ArgumentNullException(nameof(FunctionStorageAccId)) : dto.FunctionStorageAccId.Trim(),
        String.IsNullOrWhiteSpace(dto.AppServicePlanId) ? throw new ArgumentNullException(nameof(AppServicePlanId)) : dto.AppServicePlanId.Trim(),
        dto.Functions is null || !dto.Functions.Any() ? throw new ArgumentNullException(nameof(Functions)) : dto.Functions.Select(f => (AzureFunctionSettings) f).ToList());
  }
}

public record AwsSettings {
  
  public string AccountName { get; } 
  
  private AwsSettings(string accname) { AccountName = accname; }
  
  public record Dto {
    public string? AccountName { get; init; }
    
    public static explicit operator AwsSettings(Dto dto) => new (
        String.IsNullOrEmpty(dto.AccountName) ? throw new ArgumentNullException(nameof(AccountName)) : dto.AccountName.Trim());
  }
}

public record CentazioSettings {
  public string SecretsFolder { get; }
  public AwsSettings? AwsSettings { get; }
  public AzureSettings? AzureSettings { get; }
  
  private CentazioSettings (string secrets, AwsSettings? aws, AzureSettings? azure) {
    SecretsFolder = secrets;
    AwsSettings = aws;
    AzureSettings = azure;
  }
    
  public record Dto {
    public string? SecretsFolder { get; init; }
    public AwsSettings.Dto? AwsSettings { get; init; }
    public AzureSettings.Dto? AzureSettings { get; init; }
    
    public static explicit operator CentazioSettings(Dto dto) => new (
        String.IsNullOrWhiteSpace(dto.SecretsFolder) ? throw new ArgumentNullException(nameof(SecretsFolder)) : dto.SecretsFolder.Trim(),
        dto.AwsSettings is null ? null : (AwsSettings) dto.AwsSettings,
        dto.AzureSettings is null ? null : (AzureSettings) dto.AzureSettings);
  }
}
