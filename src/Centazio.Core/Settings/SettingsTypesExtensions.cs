namespace Centazio.Core.Settings;

public static class SettingsTypesExtensions {
  
  public static string GetSecretsStoreIdForEnvironment(this AwsSettings settings, string environment) {
    var template = settings.SecretsManagerStoreIdTemplate;
    if (String.IsNullOrWhiteSpace(template)) throw new Exception($"AwsSettings.SecretsManagerStoreIdTemplate is missing from settings.json file");
    return template.Replace($"<{nameof(environment)}>", environment);
  }
  

}