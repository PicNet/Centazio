using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra;

public interface ICliSecretsManager {
  Task<T> LoadSecrets<T>(string settingskey);
}

public class CliSecretsManager(IServiceProvider prov) : ICliSecretsManager {

  private ISecretsLoader GetSecretsLoader(ESecretsProviderType provider, CentazioSettings settings) {
    if (!Env.IsInDev) throw new Exception();
    
    var type = IntegrationsAssemblyInspector.GetCoreServiceFactoryType<IServiceFactory<ISecretsLoader>>(provider.ToString());
    var factory = (IServiceFactory<ISecretsLoader>) (Activator.CreateInstance(type, settings) ?? throw new Exception());
    return factory.GetService();
  }

  public async Task<T> LoadSecrets<T>(string settingskey) {
    var settings = String.IsNullOrWhiteSpace(settingskey) 
        ? prov.GetRequiredService<CentazioSettings>() 
        : prov.GetRequiredKeyedService<CentazioSettings>(settingskey);
    if (!Enum.TryParse<ESecretsProviderType>(settings.SecretsLoaderSettings.Provider, out var provider)) throw new ArgumentException($"Unknown secrets provider: {settings.SecretsLoaderSettings.Provider}");
    return await GetSecretsLoader(provider, settings).Load<T>(CentazioConstants.DEFAULT_ENVIRONMENT);
  }
}