﻿namespace Centazio.Core.Settings;

public class SettingsSectionMissingException(string section) : Exception($"{section} section missing from settings file");

public record CentazioSettings {
  public required List<string> SecretsFolders { get; init; }
  private SecretsLoaderSettings? _SecretsLoaderSettings;
  public SecretsLoaderSettings SecretsLoaderSettings => _SecretsLoaderSettings?? throw new SettingsSectionMissingException(nameof(SecretsLoaderSettings));
  
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
    _SecretsLoaderSettings = other._SecretsLoaderSettings;
    
    _Defaults = other._Defaults;
    _AwsSettings = other._AwsSettings;
    _AzureSettings = other._AzureSettings;
    _StagedEntityRepository = other._StagedEntityRepository;
    _CtlRepository = other._CtlRepository;
    _CoreStorage = other._CoreStorage;
  }
  
  public string GetSecretsFolder() => 
      Env.IsInDev 
          ? FindFirstValidDirectory(SecretsFolders) 
          : Environment.CurrentDirectory;
  
  public static string FindFirstValidDirectory(List<string> directories) => 
      directories.Select(dir => Path.IsPathFullyQualified(dir) ? dir : FsUtils.GetCentazioPath(dir)).First(Directory.Exists) 
      ?? throw new Exception($"Could not find a valid directory");

  public virtual Dto ToDto() => new() {
    SecretsFolders = SecretsFolders,
    SecretsLoaderSettings = SecretsLoaderSettings.ToDto(),
    Defaults = _Defaults?.ToDto(),
    AwsSettings = _AwsSettings?.ToDto(),
    AzureSettings = _AzureSettings?.ToDto(),
    StagedEntityRepository = _StagedEntityRepository?.ToDto(),
    CtlRepository = _CtlRepository?.ToDto(),
    CoreStorage = _CoreStorage?.ToDto()
  };
  
  public record Dto : IDto<CentazioSettings> {
    public List<string>? SecretsFolders { get; init; }
    public SecretsLoaderSettings.Dto? SecretsLoaderSettings { get; init; }
    public DefaultsSettings.Dto? Defaults { get; init; }
    public AwsSettings.Dto? AwsSettings { get; init; }
    public AzureSettings.Dto? AzureSettings { get; init; }
    public StagedEntityRepositorySettings.Dto? StagedEntityRepository { get; init; }
    public CtlRepositorySettings.Dto? CtlRepository { get; init; }
    public CoreStorageSettings.Dto? CoreStorage { get; init; }
    
    public CentazioSettings ToBase() => new() {
      SecretsFolders = SecretsFolders ?? throw new ArgumentNullException(nameof(SecretsFolders) + " wtf"),
      _SecretsLoaderSettings = SecretsLoaderSettings?.ToBase(),
      _Defaults = Defaults?.ToBase(),
      _AwsSettings = AwsSettings?.ToBase(),
      _AzureSettings = AzureSettings?.ToBase(),
      _StagedEntityRepository = StagedEntityRepository?.ToBase(),
      _CtlRepository = CtlRepository?.ToBase(),
      _CoreStorage = CoreStorage?.ToBase()
    };
  }
}
