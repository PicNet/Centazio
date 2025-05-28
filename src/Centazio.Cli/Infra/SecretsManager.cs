using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Centazio.Providers.Az.Secrets;

namespace Centazio.Cli.Infra;

public static class SecretsManager 
{
  private static readonly Dictionary<ESecretsProviderType, Func<CentazioSettings, ISecretsLoader>> Providers = new() {
    [ESecretsProviderType.File] = settings => 
        new FileSecretsLoaderFactory(settings ?? throw new ArgumentNullException(nameof(settings.SecretsFolders))).GetService(),
    [ESecretsProviderType.Aws] = settings => 
        new AwsSecretsLoaderFactory(settings ?? throw new ArgumentNullException(nameof(settings.AwsSettings))).GetService(),
    [ESecretsProviderType.Az] = settings => new AzSecretsLoaderFactory(settings  ?? throw new ArgumentNullException(nameof(settings.AzureSettings))).GetService() 
        
  };

  public static async Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments) {
    var providerString = settings.SecretsLoaderSettings.Provider ?? "File";
    if (!Enum.TryParse<ESecretsProviderType>(providerString, out var provider)) throw new ArgumentException($"Unknown secrets provider: {providerString}");

    if (!Providers.TryGetValue(provider, out var factory)) throw new ArgumentException($"Provider {provider} is not implemented");

    var loader = factory(settings);
    return await loader.Load<T>(environments.ToList());
  }
}
