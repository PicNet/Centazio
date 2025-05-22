using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public class SecretsLoaderFactory : ISecretsLoaderFactory {

  private readonly Dictionary<Provider, Func<CentazioSettings, ISecretsLoader>> _factories = new();

  public ISecretsLoader CreateSecretsLoader(Provider providerType, CentazioSettings settings) {
    if (providerType == Provider.File)
      return new SecretsFileLoader(settings.GetSecretsFolder() ?? throw new ArgumentNullException("SecretsFolder"));
    if (!_factories.TryGetValue(providerType, out var factory)) throw new ArgumentException($"No factory registered for provider type: {providerType}");
    return factory(settings);
  }

  public void RegisterProvider(Provider providerType, Func<CentazioSettings, ISecretsLoader> factory) {
    _factories[providerType] = factory;
  }

}