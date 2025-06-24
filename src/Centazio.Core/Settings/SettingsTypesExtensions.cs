namespace Centazio.Core.Settings;

public static class SettingsTypesExtensions {
  
  public static string GetSecretsStoreIdForEnvironment(this AwsSettings settings, string environment) {
    var template = settings.SecretsManagerStoreIdTemplate;
    if (String.IsNullOrWhiteSpace(template)) throw new Exception($"{nameof(AwsSettings)}.{nameof(settings.SecretsManagerStoreIdTemplate)} is missing from settings.json file");
    return template.Replace($"<{nameof(environment)}>", environment);
  }
  
  public static string GetSecretsFolder(this SecretsLoaderSettings settings) {
    if (settings.Provider != "File") throw new Exception($"{nameof(GetSecretsFolder)}() should not be called when the SecretsLoaderSettings.Provider is not 'File'");
    return Env.IsInDev ? 
        ValidateDirectory(settings.SecretsFolder) : 
        Environment.CurrentDirectory;
    
    string ValidateDirectory(string? directory) {
      if (String.IsNullOrWhiteSpace(directory)) throw new Exception($"When SecretsLoaderSettings.Provider is 'File' then `SecretsFolder` is required");
      var path = Path.IsPathFullyQualified(directory) ? directory : FsUtils.GetCentazioPath(directory);
      return Directory.Exists(path) 
          ? path 
          : throw new Exception($"Could not find a valid directory at path: {path}");
    }
  }
  

}