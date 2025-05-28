using Centazio.Core.Settings;

namespace Centazio.Providers.Az;


public static class SettingsTypesExtensions {

  public static string GetKeySecretNameForEnvironment(this AzureSettings settings, string environment) {
    var template = settings.KeySecretNameTemplate;
    if (String.IsNullOrWhiteSpace(template)) throw new Exception($"AzureSettings.KeySecretName is missing from settings.json file");
    return template.Replace($"<{nameof(environment)}>", environment);
  }

}