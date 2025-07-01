using Centazio.Core.Secrets;
using Centazio.Providers.Aws.Secrets;
using Centazio.Providers.Az.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra;

public interface ICliSecretsManager {
  Task<T> LoadSecrets<T>(string settingskey);
}

public class CliSecretsManager(IServiceProvider prov) : ICliSecretsManager {

  // todo GT: do we need this?  we already have a way of loading appropriate service factories
  private ISecretsLoader GetSecretsLoader(ESecretsProviderType provider, CentazioSettings settings) {
    if (!Env.IsInDev) throw new Exception();
    
    
    return provider switch {
      ESecretsProviderType.File => new FileSecretsLoaderFactory(settings).GetService(),
      ESecretsProviderType.Aws => new AwsSecretsLoaderFactory(settings).GetService(),
      ESecretsProviderType.Az => new AzSecretsLoaderFactory(settings).GetService(),
      _ => throw new Exception($"Provider {provider} is not implemented")
    };
  }

  public async Task<T> LoadSecrets<T>(string settingskey) {
    var settings = String.IsNullOrWhiteSpace(settingskey) 
        ? prov.GetRequiredService<CentazioSettings>() 
        : prov.GetRequiredKeyedService<CentazioSettings>(settingskey);
    if (!Enum.TryParse<ESecretsProviderType>(settings.SecretsLoaderSettings.Provider, out var provider)) throw new ArgumentException($"Unknown secrets provider: {settings.SecretsLoaderSettings.Provider}");
    return await GetSecretsLoader(provider, settings).Load<T>(CentazioConstants.DEFAULT_ENVIRONMENT);
  }
}