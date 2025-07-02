namespace Centazio.Core.Settings;

public class SettingsSectionMissingException(string section) : Exception($"{section} section missing from settings file");

public record CentazioSettings {
  private readonly SecretsLoaderSettings? _SecretsLoaderSettings;
  public SecretsLoaderSettings SecretsLoaderSettings { get => _SecretsLoaderSettings ?? throw new SettingsSectionMissingException(nameof(SecretsLoaderSettings)); internal init => _SecretsLoaderSettings = value; }
  private readonly DefaultsSettings? _Defaults;
  public DefaultsSettings Defaults => _Defaults ?? throw new SettingsSectionMissingException(nameof(Defaults));
  
  private readonly AwsSettings? _AwsSettings;
  public AwsSettings AwsSettings => _AwsSettings ?? throw new SettingsSectionMissingException(nameof(AwsSettings));
  
  private readonly AzureSettings? _AzureSettings;
  public AzureSettings AzureSettings { get => _AzureSettings ?? throw new SettingsSectionMissingException(nameof(AzureSettings)); internal init => _AzureSettings = value; }
  private readonly StagedEntityRepositorySettings? _StagedEntityRepository;
  public StagedEntityRepositorySettings StagedEntityRepository => _StagedEntityRepository ?? throw new SettingsSectionMissingException(nameof(StagedEntityRepository));
  
  private readonly CtlRepositorySettings? _CtlRepository;
  public CtlRepositorySettings CtlRepository => _CtlRepository ?? throw new SettingsSectionMissingException(nameof(CtlRepository));
  
  private readonly CoreStorageSettings? _CoreStorage;
  public CoreStorageSettings CoreStorage => _CoreStorage ?? throw new SettingsSectionMissingException(nameof(CoreStorage));
  
  protected CentazioSettings(CentazioSettings other) {
    _SecretsLoaderSettings = other._SecretsLoaderSettings;
    
    _Defaults = other._Defaults;
    _AwsSettings = other._AwsSettings;
    _AzureSettings = other._AzureSettings;
    _StagedEntityRepository = other._StagedEntityRepository;
    _CtlRepository = other._CtlRepository;
    _CoreStorage = other._CoreStorage;
  }
}
