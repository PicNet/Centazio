using Centazio.Core.Secrets;
using Centazio.Providers.Aws.Secrets;
using Centazio.Providers.Az.Secrets;

namespace Centazio.Cli.Infra;

public static class SecretsManager 
{
  public delegate ISecretsLoader SecretsLoaderFactory(CentazioSettings settings);
  
  private static readonly Dictionary<ESecretsProviderType, SecretsLoaderFactory> Providers = new() {
    [ESecretsProviderType.File] = settings => new FileSecretsLoaderFactory(settings).GetService(),
    [ESecretsProviderType.Aws] = settings => new AwsSecretsLoaderFactory(settings).GetService(),
    [ESecretsProviderType.Az] = settings => new AzSecretsLoaderFactory(settings).GetService() 
  };

  public static async Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments) {
    var provname = settings.SecretsLoaderSettings.Provider;
    if (!Enum.TryParse<ESecretsProviderType>(provname, out var provider)) throw new ArgumentException($"Unknown secrets provider: {provname}");
    if (!Providers.TryGetValue(provider, out var factory)) throw new ArgumentException($"Provider {provider} is not implemented");

    var loader = factory(settings);
    return await loader.Load<T>(environments.ToList());
  }
}
