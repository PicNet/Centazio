using System.Text.RegularExpressions;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Stage;

namespace Centazio.Core.Settings;

public class SettingsSectionMissingException(string section) : Exception($"{section} section missing from settings file");

public record CentazioSettings {
  public required List<string> SecretsFolders { get; init; }
  
  private DefaultsSettings? _Defaults;
  public DefaultsSettings Defaults => _Defaults ?? throw new SettingsSectionMissingException(nameof(Defaults));
  
  private AwsSettings? _AwsSettings;
  public AwsSettings AwsSettings => _AwsSettings ?? throw new SettingsSectionMissingException(nameof(AwsSettings));
  
  private AzureSettings? _AzureSettings;
  public AzureSettings AzureSettings => _AzureSettings ?? throw new SettingsSectionMissingException(nameof(AzureSettings));
  
  private StagedEntityRepositorySettings? _StagedEntityRepository;
  public StagedEntityRepositorySettings StagedEntityRepository => _StagedEntityRepository ?? throw new SettingsSectionMissingException(nameof(StagedEntityRepository));
  
  private CtlRepositorySettings? _CtlRepository;
  public CtlRepositorySettings CtlRepository => _CtlRepository ?? throw new SettingsSectionMissingException(nameof(CtlRepository));
  
  private CoreStorageSettings? _CoreStorage;
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
  
  public string GetSecretsFolder() => 
      CloudUtils.IsCloudEnviornment() 
          ? Environment.CurrentDirectory 
          : FsUtils.FindFirstValidDirectory(SecretsFolders);

  public virtual Dto ToDto() => new() {
      SecretsFolders = SecretsFolders,
      Defaults = _Defaults?.ToDto(),
      AwsSettings = _AwsSettings?.ToDto(),
      AzureSettings = _AzureSettings?.ToDto(),
      StagedEntityRepository = _StagedEntityRepository?.ToDto(),
      CtlRepository = _CtlRepository?.ToDto(),
      CoreStorage = _CoreStorage?.ToDto()
    };
  
  public record Dto : IDto<CentazioSettings> {
    public List<string>? SecretsFolders { get; init; }
    
    public DefaultsSettings.Dto? Defaults { get; init; }
    public AwsSettings.Dto? AwsSettings { get; init; }
    public AzureSettings.Dto? AzureSettings { get; init; }
    public StagedEntityRepositorySettings.Dto? StagedEntityRepository { get; init; }
    public CtlRepositorySettings.Dto? CtlRepository { get; init; }
    public CoreStorageSettings.Dto? CoreStorage { get; init; }
    
    public CentazioSettings ToBase() => new() {
      SecretsFolders = SecretsFolders is null || !SecretsFolders.Any() ? throw new ArgumentNullException(nameof(SecretsFolders)) : SecretsFolders,
      _Defaults = Defaults?.ToBase(),
      _AwsSettings = AwsSettings?.ToBase(),
      _AzureSettings = AzureSettings?.ToBase(),
      _StagedEntityRepository = StagedEntityRepository?.ToBase(),
      _CtlRepository = CtlRepository?.ToBase(),
      _CoreStorage = CoreStorage?.ToBase()
    };
  }

  public string Parse(string command, object? args=null, CentazioSecrets? secrets=null) {
    var macros = Regex.Matches(command, @"(\[[\w.]+\])");
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

public record AzureFunctionSettings {
  public required string FunctionAppId { get; init; } 
  public required string FunctionClassName { get; init; }
  
  public Dto ToDto() => new() {
    FunctionAppId = FunctionAppId,
    FunctionClassName = FunctionClassName
  };

  public record Dto : IDto<AzureFunctionSettings> {
    public string? FunctionAppId { get; init; }
    public string? FunctionClassName { get; init; }
    
    public AzureFunctionSettings ToBase() => new() {
      FunctionAppId = String.IsNullOrWhiteSpace(FunctionAppId) ? throw new ArgumentNullException(nameof(FunctionAppId)) : FunctionAppId.Trim(),
      FunctionClassName = String.IsNullOrWhiteSpace(FunctionClassName) ? throw new ArgumentNullException(nameof(FunctionClassName)) : FunctionClassName.Trim()
    };
  }

}

public record AzureSettings {
  public required string Region { get; init; }
  public required string ResourceGroup { get; init; }
  public List<AzureFunctionSettings>? Functions { get; init; }
  
  public Dto ToDto() => new() {
    Region = Region,
    ResourceGroup = ResourceGroup,
    Functions = Functions?.Select(f => f.ToDto()).ToList()
  };

  public record Dto : IDto<AzureSettings> {
    public string? Region { get; init; }
    public string? ResourceGroup { get; init; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<AzureFunctionSettings.Dto>? Functions { get; init; }
    
    public AzureSettings ToBase() => new() {
      Region = String.IsNullOrWhiteSpace(Region) ? throw new ArgumentNullException(nameof(Region)) : Region.Trim(),
      ResourceGroup = String.IsNullOrWhiteSpace(ResourceGroup) ? throw new ArgumentNullException(nameof(ResourceGroup)) : ResourceGroup.Trim(),
      Functions = Functions is null ? [] : Functions.Select(f => f.ToBase()).ToList() 
    };
  }

}

public record DefaultsSettings {
  
  public required string GeneratedCodeFolder { get; init; }
  
  public required string DotNetCleanProject { get; init; }
  public required string DotNetBuildProject { get; init; }
  public required string DotNetPublishProject { get; init; }
  
  public required string AzListFunctionAppsCmd { get; init; }
  public required string AzListFunctionsCmd { get; init; }
  public required string AzDeleteFunctionAppCmd { get; init; }

  public Dto ToDto() {
    return new () {
      GeneratedCodeFolder = GeneratedCodeFolder,
      
      DotNetCleanProject = DotNetCleanProject,
      DotNetBuildProject = DotNetBuildProject,
      DotNetPublishProject = DotNetPublishProject,
      
      AzListFunctionAppsCmd = AzListFunctionAppsCmd,
      AzListFunctionsCmd = AzListFunctionsCmd,
      AzDeleteFunctionAppCmd = AzDeleteFunctionAppCmd,
    };
  }

  public record Dto : IDto<DefaultsSettings> {
    public string? GeneratedCodeFolder { get; init; }
    
    public string? DotNetCleanProject { get; init; }
    public string? DotNetBuildProject { get; init; }
    public string? DotNetPublishProject { get; init; }
  
    public string? AzListFunctionAppsCmd { get; init; }
    public string? AzListFunctionsCmd { get; init;  }
    public string? AzDeleteFunctionAppCmd { get; init; }
    
    public DefaultsSettings ToBase() => new() {
      GeneratedCodeFolder = String.IsNullOrEmpty(GeneratedCodeFolder) ? throw new ArgumentNullException(nameof(GeneratedCodeFolder)) : GeneratedCodeFolder.Trim(),
      
      DotNetCleanProject = String.IsNullOrEmpty(DotNetCleanProject) ? throw new ArgumentNullException(nameof(DotNetCleanProject)) : DotNetCleanProject.Trim(),
      DotNetBuildProject = String.IsNullOrEmpty(DotNetBuildProject) ? throw new ArgumentNullException(nameof(DotNetBuildProject)) : DotNetBuildProject.Trim(),
      DotNetPublishProject = String.IsNullOrEmpty(DotNetPublishProject) ? throw new ArgumentNullException(nameof(DotNetPublishProject)) : DotNetPublishProject.Trim(),
      
      AzListFunctionAppsCmd = String.IsNullOrEmpty(AzListFunctionAppsCmd) ? throw new ArgumentNullException(nameof(AzListFunctionAppsCmd)) : AzListFunctionAppsCmd.Trim(),
      AzListFunctionsCmd = String.IsNullOrEmpty(AzListFunctionsCmd) ? throw new ArgumentNullException(nameof(AzListFunctionsCmd)) : AzListFunctionsCmd.Trim(),
      AzDeleteFunctionAppCmd = String.IsNullOrEmpty(AzDeleteFunctionAppCmd) ? throw new ArgumentNullException(nameof(AzDeleteFunctionAppCmd)) : AzDeleteFunctionAppCmd.Trim()
    };
  }

}

public record AwsSettings {
  
  public required string AccountName { get; init; } 
  
  public Dto ToDto() {
    return new () {
      AccountName = AccountName
    };
  }

  public record Dto : IDto<AwsSettings> {
    public string? AccountName { get; init; }
    
    public AwsSettings ToBase() => new() {
      AccountName  = String.IsNullOrEmpty(AccountName) ? throw new ArgumentNullException(nameof(AccountName)) : AccountName.Trim()
    };
  }

}

public record StagedEntityRepositorySettings {
  public required string Provider { get; init; }
  public required string ConnectionString { get; init; }
  public required string SchemaName { get; init; }
  public required string TableName { get; init; }
  public required int Limit { get; init; }
  public required bool CreateSchema { get; init; }
  
  public Dto ToDto() => new() {
    Provider = Provider,
    CreateSchema = CreateSchema,
    SchemaName = SchemaName,
    TableName = TableName,
    Limit = Limit,
    ConnectionString = ConnectionString
  };

  public record Dto : IDto<StagedEntityRepositorySettings> {
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public string? SchemaName { get; init; }
    public string? TableName { get; init; }
    public int? Limit { get; init; }
    public bool? CreateSchema { get; init; }
    
    public StagedEntityRepositorySettings ToBase() => new() {
      Provider = String.IsNullOrWhiteSpace(Provider) ? throw new ArgumentNullException(nameof(Provider)) : Provider.Trim(),
      ConnectionString = String.IsNullOrEmpty(ConnectionString) ? throw new ArgumentNullException(nameof(ConnectionString)) : ConnectionString.Trim(),
      SchemaName = SchemaName?.Trim() ?? nameof(Ctl).ToLower(),
      TableName = TableName?.Trim() ?? nameof(StagedEntity).ToLower(),
      Limit = Limit ?? 0,
      CreateSchema = CreateSchema ?? false
    };
  }

}

public record CtlRepositorySettings {
  public required string Provider { get; init; }
  public required string ConnectionString { get; init; }
  public required string SchemaName { get; init; }
  public required string SystemStateTableName { get; init; }
  public required string ObjectStateTableName { get; init; }
  public required string CoreToSysMapTableName { get; init; }
  public required bool CreateSchema { get; init; }
  
  public Dto ToDto() => new() {
    ConnectionString = ConnectionString,
    SchemaName = SchemaName,
    CreateSchema = CreateSchema,
    ObjectStateTableName = ObjectStateTableName,
    SystemStateTableName = SystemStateTableName,
    CoreToSysMapTableName = CoreToSysMapTableName,
    Provider = Provider
  };

  public record Dto : IDto<CtlRepositorySettings> {
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public string? SchemaName { get; init; }
    public string? SystemStateTableName { get; init; }
    public string? ObjectStateTableName { get; init; }
    public string? CoreToSysMapTableName { get; init; }
    public bool? CreateSchema { get; init; }
    
    public CtlRepositorySettings ToBase() => new() {
      Provider = String.IsNullOrWhiteSpace(Provider) ? throw new ArgumentNullException(nameof(Provider)) : Provider.Trim(),
      ConnectionString = String.IsNullOrEmpty(ConnectionString) ? throw new ArgumentNullException(nameof(ConnectionString)) : ConnectionString.Trim(),
      SchemaName = SchemaName?.Trim() ?? nameof(Ctl).ToLower(),
      SystemStateTableName = SystemStateTableName?.Trim() ?? nameof(SystemState).ToLower(),
      ObjectStateTableName = ObjectStateTableName?.Trim() ?? nameof(ObjectState).ToLower(),
      CoreToSysMapTableName = CoreToSysMapTableName?.Trim() ?? nameof(Map.CoreToSysMap).ToLower(),
      CreateSchema = CreateSchema ?? false
    };
  }

}

public record CoreStorageSettings {
  public required string Provider { get; init; }
  public required string ConnectionString { get; init; }
  public required string SchemaName { get; init; }
  public required string CtlSchemaName { get; init; }
  public required bool CreateSchema { get; init; }
  
  public Dto ToDto() => new() {
    Provider = Provider,
    CreateSchema = CreateSchema,
    SchemaName = SchemaName,
    ConnectionString = ConnectionString,
    CtlSchemaName = CtlSchemaName
  };

  public record Dto : IDto<CoreStorageSettings> {
    public string? Provider { get; init; }
    public string? ConnectionString { get; init; }
    public string? SchemaName { get; init; }
    public string? CtlSchemaName { get; init; }
    public bool? CreateSchema { get; init; }
    
    public CoreStorageSettings ToBase() => new() {
      Provider = String.IsNullOrWhiteSpace(Provider) ? throw new ArgumentNullException(nameof(Provider)) : Provider.Trim(),
      ConnectionString = String.IsNullOrEmpty(ConnectionString) ? throw new ArgumentNullException(nameof(ConnectionString)) : ConnectionString.Trim(),
      SchemaName = SchemaName?.Trim() ?? nameof(Ctl).ToLower(),
      CtlSchemaName = CtlSchemaName?.Trim() ?? nameof(CtlSchemaName).ToLower(),
      CreateSchema = CreateSchema ?? false
    };
  }

}