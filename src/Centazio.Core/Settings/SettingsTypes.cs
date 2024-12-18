using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
// ReSharper disable UnusedAutoPropertyAccessor.Global

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
    // ReSharper disable once CollectionNeverUpdated.Global
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

public record StagedEntityRepositorySettings {
  public string Provider { get; }
  public string ConnectionString { get; }
  public string SchemaName { get; }
  public string TableName { get; }
  public int Limit { get; }
  public bool CreateSchema { get; }
  
  private StagedEntityRepositorySettings(string provider, string connstr, string schemanm, string tablenm, int limit, bool create) {
    Provider = provider;
    ConnectionString = connstr;
    SchemaName = schemanm;
    TableName = tablenm;
    Limit = limit;
    CreateSchema = create;
  } 
  
  public record Dto : IDto<StagedEntityRepositorySettings> {
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public string? SchemaName { get; init; }
    public string? TableName { get; init; }
    public int? Limit { get; init; }
    public bool? CreateSchema { get; init; }
    
    public StagedEntityRepositorySettings ToBase() => new (
        String.IsNullOrWhiteSpace(Provider) ? throw new ArgumentNullException(nameof(Provider)) : Provider.Trim(),
        String.IsNullOrEmpty(ConnectionString) ? throw new ArgumentNullException(nameof(ConnectionString)) : ConnectionString.Trim(),
        SchemaName?.Trim() ?? nameof(Ctl).ToLower(),
        TableName?.Trim() ?? nameof(StagedEntity).ToLower(),
        Limit ?? 0,
        CreateSchema ?? false);
  }
}

public record CtlRepositorySettings {
  public string Provider { get; }
  public string ConnectionString { get; }
  public string SchemaName { get; }
  public string SystemStateTableName { get; }
  public string ObjectStateTableName { get; }
  public string CoreToSysMapTableName { get; }
  public bool CreateSchema { get; }
  
  private CtlRepositorySettings(string provider, string connstr, string schemanm, string systemstatenm, string objectstatenm, string coretosysmapnm, bool create) {
    Provider = provider;
    ConnectionString = connstr;
    SchemaName = schemanm;
    SystemStateTableName = systemstatenm;
    ObjectStateTableName = objectstatenm;
    CoreToSysMapTableName = coretosysmapnm;
    CreateSchema = create;
  } 
  
  public record Dto : IDto<CtlRepositorySettings> {
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public string? SchemaName { get; init; }
    public string? SystemStateTableName { get; init; }
    public string? ObjectStateTableName { get; init; }
    public string? CoreToSysMapTableName { get; init; }
    public bool? CreateSchema { get; init; }
    
    public CtlRepositorySettings ToBase() => new (
        String.IsNullOrWhiteSpace(Provider) ? throw new ArgumentNullException(nameof(Provider)) : Provider.Trim(),
        String.IsNullOrEmpty(ConnectionString) ? throw new ArgumentNullException(nameof(ConnectionString)) : ConnectionString.Trim(),
        SchemaName?.Trim() ?? nameof(Ctl).ToLower(),
        SystemStateTableName?.Trim() ?? nameof(SystemState).ToLower(),
        ObjectStateTableName?.Trim() ?? nameof(ObjectState).ToLower(),
        CoreToSysMapTableName?.Trim() ?? nameof(Map.CoreToSysMap).ToLower(),
        CreateSchema ?? false);
  }
}


public record CentazioSettings {
  public List<string> SecretsFolders { get; }
  public List<string> AllowedFunctionAssemblies { get; }
  public List<string> AllowedProviderAssemblies { get; }
  
  private readonly AwsSettings? _AwsSettings;
  public AwsSettings AwsSettings => _AwsSettings ?? throw new Exception($"AwsSettings section missing from CentazioSettings");
  
  private readonly AzureSettings? _AzureSettings;
  public AzureSettings AzureSettings => _AzureSettings ?? throw new Exception($"AzureSettings section missing from CentazioSettings");
  
  private readonly StagedEntityRepositorySettings? _StagedEntityRepository;
  public StagedEntityRepositorySettings StagedEntityRepository => _StagedEntityRepository ?? throw new Exception($"StagedEntityRepository section missing from CentazioSettings");
  
  private readonly CtlRepositorySettings? _CtlRepository;
  public CtlRepositorySettings CtlRepository => _CtlRepository ?? throw new Exception($"CtlRepository section missing from CentazioSettings");
  
  protected CentazioSettings (CentazioSettings other) {
    SecretsFolders = other.SecretsFolders;
    AllowedFunctionAssemblies = other.AllowedFunctionAssemblies;
    AllowedProviderAssemblies = other.AllowedProviderAssemblies;
    
    _AwsSettings = other._AwsSettings;
    _AzureSettings = other._AzureSettings;
    _StagedEntityRepository = other._StagedEntityRepository;
    _CtlRepository = other._CtlRepository;
  }
  
  private CentazioSettings (List<string> secrets, List<string> funcass, List<string> provass, AwsSettings? aws, AzureSettings? azure, StagedEntityRepositorySettings? staged, CtlRepositorySettings? ctlrepo) {
    SecretsFolders = secrets;
    AllowedFunctionAssemblies = funcass;
    AllowedProviderAssemblies = provass;
    
    _AwsSettings = aws;
    _AzureSettings = azure;
    _StagedEntityRepository = staged;
    _CtlRepository = ctlrepo;
  }

  public string GetSecretsFolder() => FsUtils.FindFirstValidDirectory(SecretsFolders);

  public record Dto : IDto<CentazioSettings> {
    public List<string>? SecretsFolders { get; init; }
    public List<string>? AllowedFunctionAssemblies { get; init; }
    public List<string>? AllowedProviderAssemblies { get; init; }
    public AwsSettings.Dto? AwsSettings { get; init; }
    public AzureSettings.Dto? AzureSettings { get; init; }
    public StagedEntityRepositorySettings.Dto? StagedEntityRepository { get; init; }
    public CtlRepositorySettings.Dto? CtlRepository { get; init; }
    
    public CentazioSettings ToBase() => new (
        SecretsFolders is null || !SecretsFolders.Any() ? throw new ArgumentNullException(nameof(SecretsFolders)) : SecretsFolders,
        AllowedFunctionAssemblies ?? [nameof(Centazio)],
        AllowedProviderAssemblies ?? [nameof(Centazio)],
        AwsSettings?.ToBase(),
        AzureSettings?.ToBase(),
        StagedEntityRepository?.ToBase(),
        CtlRepository?.ToBase());
  }

}
