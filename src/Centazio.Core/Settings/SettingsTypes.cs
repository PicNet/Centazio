namespace Centazio.Core.Settings;

public record AzureFunctionSettings {
  public string FunctionAppId { get; } 
  public string FunctionClassName { get; }
  
  private AzureFunctionSettings(string appid, string classname) {
    FunctionAppId = appid;
    FunctionClassName = classname;
  }
  
  public record Dto : IDto<AzureFunctionSettings> {
    public string? FunctionAppId { get; init; }
    public string? FunctionClassName { get; init; }
    
    public AzureFunctionSettings ToBase() => new (
        String.IsNullOrWhiteSpace(FunctionAppId) ? throw new ArgumentNullException(nameof(FunctionAppId)) : FunctionAppId.Trim(),
        String.IsNullOrWhiteSpace(FunctionClassName) ? throw new ArgumentNullException(nameof(FunctionClassName)) : FunctionClassName.Trim());
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
  
  
  public record Dto : IDto<AzureSettings> {
    public string? Region { get; init; }
    public string? ResourceGroup { get; init; }
    public string? FunctionStorageAccId { get; init; }
    public string? AppServicePlanId { get; init; }
    public List<AzureFunctionSettings.Dto>? Functions { get; init; }
    
    public AzureSettings ToBase() => new (
        String.IsNullOrWhiteSpace(Region) ? throw new ArgumentNullException(nameof(Region)) : Region.Trim(),
        String.IsNullOrWhiteSpace(ResourceGroup) ? throw new ArgumentNullException(nameof(ResourceGroup)) : ResourceGroup.Trim(),
        String.IsNullOrWhiteSpace(FunctionStorageAccId) ? throw new ArgumentNullException(nameof(FunctionStorageAccId)) : FunctionStorageAccId.Trim(),
        String.IsNullOrWhiteSpace(AppServicePlanId) ? throw new ArgumentNullException(nameof(AppServicePlanId)) : AppServicePlanId.Trim(),
        Functions is null || !Functions.Any() ? throw new ArgumentNullException(nameof(Functions)) : Functions.Select(f => f.ToBase()).ToList());
  }
}

public record AwsSettings {
  
  public string AccountName { get; } 
  
  private AwsSettings(string accname) { AccountName = accname; }
  
  public record Dto : IDto<AwsSettings> {
    public string? AccountName { get; init; }
    
    public AwsSettings ToBase() => new (
        String.IsNullOrEmpty(AccountName) ? throw new ArgumentNullException(nameof(AccountName)) : AccountName.Trim());
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
    
  public record Dto : IDto<CentazioSettings> {
    public string? SecretsFolder { get; init; }
    public AwsSettings.Dto? AwsSettings { get; init; }
    public AzureSettings.Dto? AzureSettings { get; init; }
    
    public CentazioSettings ToBase() => new (
        String.IsNullOrWhiteSpace(SecretsFolder) ? throw new ArgumentNullException(nameof(SecretsFolder)) : SecretsFolder.Trim(),
        AwsSettings?.ToBase(),
        AzureSettings?.ToBase());
  }
}
