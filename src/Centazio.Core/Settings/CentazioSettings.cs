namespace Centazio.Core.Settings;

public record CentazioSettings(SecretsLoaderSettings SecretsLoaderSettings, DefaultsSettings Defaults, AwsSettings AwsSettings, AzureSettings AzureSettings, StagedEntityRepositorySettings StagedEntityRepository, CtlRepositorySettings CtlRepository, CoreStorageSettings CoreStorage) {
  public CentazioSettings() : this(null!, null!, null!, null!, null!, null!, null!) {}
}
