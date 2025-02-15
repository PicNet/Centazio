using System.Text.RegularExpressions;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Stage;

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
  public List<AzureFunctionSettings>? Functions { get; }
  
  private AzureSettings(string region, string resourcegroup, List<AzureFunctionSettings>? functions) {
    Region = region;  
    ResourceGroup = resourcegroup;
    Functions = functions;
  }
  
  
  public record Dto : IDto<AzureSettings> {
    public string? Region { get; init; }
    public string? ResourceGroup { get; init; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<AzureFunctionSettings.Dto>? Functions { get; init; }
    
    public AzureSettings ToBase() => new (
        String.IsNullOrWhiteSpace(Region) ? throw new ArgumentNullException(nameof(Region)) : Region.Trim(),
        String.IsNullOrWhiteSpace(ResourceGroup) ? throw new ArgumentNullException(nameof(ResourceGroup)) : ResourceGroup.Trim(),
        Functions is null ? [] : Functions.Select(f => f.ToBase()).ToList());
  }
}

public record DefaultsSettings {
  
  public string GeneratedCodeFolder { get; }
  
  public string DotNetCleanProject { get; }
  public string DotNetBuildProject { get; }
  public string DotNetPublishProject { get; }
  
  public string AzListFunctionAppsCmd { get; }
  public string AzListFunctionsCmd { get; }
  public string AzDeleteFunctionAppCmd { get; }
  
  private DefaultsSettings(string gencode, string dotnetclean, string dotnetbuild, string dotnetpublish, string azlstfuncapps, string azlstfuncs, string azdelfuncapp) { 
    GeneratedCodeFolder = gencode; 
    
    DotNetCleanProject = dotnetclean;
    DotNetBuildProject = dotnetbuild;
    DotNetPublishProject=dotnetpublish;
    
    AzListFunctionAppsCmd = azlstfuncapps;
    AzListFunctionsCmd = azlstfuncs;
    AzDeleteFunctionAppCmd = azdelfuncapp;
  }
  
  public record Dto : IDto<DefaultsSettings> {
    public string? GeneratedCodeFolder { get; init; }
    
    public string? DotNetCleanProject { get; init; }
    public string? DotNetBuildProject { get; init; }
    public string? DotNetPublishProject { get; init; }
  
    public string? AzListFunctionAppsCmd { get; init; }
    public string? AzListFunctionsCmd { get; init;  }
    public string? AzDeleteFunctionAppCmd { get; init; }
    
    public DefaultsSettings ToBase() => new (
        String.IsNullOrEmpty(GeneratedCodeFolder) ? throw new ArgumentNullException(nameof(GeneratedCodeFolder)) : GeneratedCodeFolder.Trim(),
        
        String.IsNullOrEmpty(DotNetCleanProject) ? throw new ArgumentNullException(nameof(DotNetCleanProject)) : DotNetCleanProject.Trim(),
        String.IsNullOrEmpty(DotNetBuildProject) ? throw new ArgumentNullException(nameof(DotNetBuildProject)) : DotNetBuildProject.Trim(),
        String.IsNullOrEmpty(DotNetPublishProject) ? throw new ArgumentNullException(nameof(DotNetPublishProject)) : DotNetPublishProject.Trim(),
        
        String.IsNullOrEmpty(AzListFunctionAppsCmd) ? throw new ArgumentNullException(nameof(AzListFunctionAppsCmd)) : AzListFunctionAppsCmd.Trim(),
        String.IsNullOrEmpty(AzListFunctionsCmd) ? throw new ArgumentNullException(nameof(AzListFunctionsCmd)) : AzListFunctionsCmd.Trim(),
        String.IsNullOrEmpty(AzDeleteFunctionAppCmd) ? throw new ArgumentNullException(nameof(AzDeleteFunctionAppCmd)) : AzDeleteFunctionAppCmd.Trim());
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

public record CoreStorageSettings {
  public string Provider { get; }
  public string ConnectionString { get; }
  public string SchemaName { get; }
  public string CtlSchemaName { get; }
  public bool CreateSchema { get; }
  
  private CoreStorageSettings(string provider, string connstr, string schemanm, string ctlschema, bool create) {
    Provider = provider;
    ConnectionString = connstr;
    SchemaName = schemanm;
    CtlSchemaName = ctlschema;
    CreateSchema = create;
  } 
  
  public record Dto : IDto<CoreStorageSettings> {
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public string? SchemaName { get; init; }
    public string? CtlSchemaName { get; init; }
    public bool? CreateSchema { get; init; }
    
    public CoreStorageSettings ToBase() => new (
        String.IsNullOrWhiteSpace(Provider) ? throw new ArgumentNullException(nameof(Provider)) : Provider.Trim(),
        String.IsNullOrEmpty(ConnectionString) ? throw new ArgumentNullException(nameof(ConnectionString)) : ConnectionString.Trim(),
        SchemaName?.Trim() ?? nameof(Ctl).ToLower(),
        CtlSchemaName?.Trim() ?? nameof(CtlSchemaName).ToLower(),
        CreateSchema ?? false);
  }
}

public record CentazioSettings {
  public List<string> SecretsFolders { get; }
  
  private readonly DefaultsSettings? _Defaults;
  public DefaultsSettings Defaults => _Defaults ?? throw new SettingsSectionMissingException(nameof(Defaults));
  
  private readonly AwsSettings? _AwsSettings;
  public AwsSettings AwsSettings => _AwsSettings ?? throw new SettingsSectionMissingException(nameof(AwsSettings));
  
  private readonly AzureSettings? _AzureSettings;
  public AzureSettings AzureSettings => _AzureSettings ?? throw new SettingsSectionMissingException(nameof(AzureSettings));
  
  private readonly StagedEntityRepositorySettings? _StagedEntityRepository;
  public StagedEntityRepositorySettings StagedEntityRepository => _StagedEntityRepository ?? throw new SettingsSectionMissingException(nameof(StagedEntityRepository));
  
  private readonly CtlRepositorySettings? _CtlRepository;
  public CtlRepositorySettings CtlRepository => _CtlRepository ?? throw new SettingsSectionMissingException(nameof(CtlRepository));
  
  private readonly CoreStorageSettings? _CoreStorage;
  public CoreStorageSettings CoreStorage => _CoreStorage ?? throw new SettingsSectionMissingException(nameof(CoreStorage));
  
  protected CentazioSettings(CentazioSettings other) {
    SecretsFolders = other.SecretsFolders;
    
    _Defaults = other._Defaults;
    _AwsSettings = other._AwsSettings;
    _AzureSettings = other._AzureSettings;
    _StagedEntityRepository = other._StagedEntityRepository;
    _CtlRepository = other._CtlRepository;
    _CoreStorage = other._CoreStorage;
  }
  
  private CentazioSettings (List<string> secrets, DefaultsSettings? defaults, AwsSettings? aws, AzureSettings? azure, StagedEntityRepositorySettings? staged, CtlRepositorySettings? ctlrepo, CoreStorageSettings? core) {
    SecretsFolders = secrets;
    
    _Defaults = defaults;
    _AwsSettings = aws;
    _AzureSettings = azure;
    _StagedEntityRepository = staged;
    _CtlRepository = ctlrepo;
    _CoreStorage = core;
  }

  public string GetSecretsFolder() => FsUtils.FindFirstValidDirectory(SecretsFolders);

  public record Dto : IDto<CentazioSettings> {
    public List<string>? SecretsFolders { get; init; }
    
    public DefaultsSettings.Dto? Defaults { get; init; }
    public AwsSettings.Dto? AwsSettings { get; init; }
    public AzureSettings.Dto? AzureSettings { get; init; }
    public StagedEntityRepositorySettings.Dto? StagedEntityRepository { get; init; }
    public CtlRepositorySettings.Dto? CtlRepository { get; init; }
    public CoreStorageSettings.Dto? CoreStorage { get; init; }
    
    public CentazioSettings ToBase() => new (
        SecretsFolders is null || !SecretsFolders.Any() ? throw new ArgumentNullException(nameof(SecretsFolders)) : SecretsFolders,
        Defaults?.ToBase(),
        AwsSettings?.ToBase(),
        AzureSettings?.ToBase(),
        StagedEntityRepository?.ToBase(),
        CtlRepository?.ToBase(),
        CoreStorage?.ToBase());
  }

  public class SettingsSectionMissingException(string section) : Exception($"{section} section missing from settings file");

  public string Parse(string command, object? args=null, CentazioSecrets? secrets=null) {
    var macros = Regex.Matches(command, @"(\[[\w.]*\])");
    macros.ForEach(m => command = command.Replace(m.Groups[0].Value, ParseMacroValue(m.Groups[0].Value[1..^1])));
    return command;
    
    string ParseMacroValue(string val) {
      if (val.StartsWith("settings.")) return ReflectionUtils.ParseStrValue(this, val.Replace("settings.", String.Empty));
      if (val.StartsWith("secrets.")) return ReflectionUtils.ParseStrValue(secrets ?? throw new ArgumentNullException(nameof(secrets)), val.Replace("secrets.", String.Empty));
      return ReflectionUtils.ParseStrValue(args ?? throw new ArgumentNullException(nameof(args)), val);
    }
  }

  public string Template(string path, object? args=null) {
    var contents = File.ReadAllText(FsUtils.GetSolutionFilePath("defaults", "templates", path));
    return Parse(contents, args);
  }

}
