using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra;

public interface ICliSecretsManager {
  Task<T> LoadSecrets<T>(string settingskey);
}

public class CliSecretsManager(IServiceProvider prov) : ICliSecretsManager {

  public async Task<T> LoadSecrets<T>(string settingskey) {
    if (!Env.IsInDev) throw new Exception();
    var settings = GetSettingsFromKey(settingskey);
    return await GetSecretsLoader(settings).Load<T>(CentazioConstants.DEFAULT_ENVIRONMENT);
  }

  internal CentazioSettings GetSettingsFromKey(string settingskey) => 
      String.IsNullOrWhiteSpace(settingskey) 
          ? prov.GetRequiredService<CentazioSettings>() 
          : prov.GetRequiredKeyedService<CentazioSettings>(settingskey);

  internal ISecretsLoader GetSecretsLoader(CentazioSettings settings) {
    if (!Enum.TryParse<ESecretsProviderType>(settings.SecretsLoaderSettings.Provider, out var provider)) throw new ArgumentException($"Unknown secrets provider: {settings.SecretsLoaderSettings.Provider}");
    
    var type = IntegrationsAssemblyInspector.GetCoreServiceFactoryType<IServiceFactory<ISecretsLoader>>(provider.ToString());
    var factory = (IServiceFactory<ISecretsLoader>) (Activator.CreateInstance(type, settings) ?? throw new Exception());
    return factory.GetService();
  }

}