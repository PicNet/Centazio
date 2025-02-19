using System.Text.RegularExpressions;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;

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
