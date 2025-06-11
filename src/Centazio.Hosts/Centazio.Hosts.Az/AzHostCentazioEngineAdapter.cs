using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Centazio.Providers.Az.Secrets;

namespace Centazio.Hosts.Az;

public class AzHostCentazioEngineAdapter(CentazioSettings settings, List<string> environments) : CentazioEngine(environments) {

  private readonly List<string> environments = environments;
  private readonly Dictionary<ESecretsProviderType, Func<ISecretsLoader>> Providers = new() {
    [ESecretsProviderType.File] = () => new FileSecretsLoaderFactory(settings).GetService(),
    [ESecretsProviderType.Az] = () => new AzSecretsLoaderFactory(settings).GetService(),
    [ESecretsProviderType.Aws] = () => new AwsSecretsLoaderFactory(settings).GetService()
  };

  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo WT: this should support function-to-function triggers
    var providersetting = settings.SecretsLoaderSettings.Provider;
    if (!Enum.TryParse<ESecretsProviderType>(providersetting, out var provider))
      throw new ArgumentException($"Unknown secrets provider: {providersetting}");

    if (!Providers.TryGetValue(provider, out var factory))
      throw new ArgumentException($"Provider {provider} is not implemented");

    var loader = factory();
    var secrets = loader.Load<CentazioSecrets>(environments).Result;

        
    // Register services
    registrar.Register(secrets);
    registrar.Register<ISecretsLoader>(_ => loader);
    registrar.Register<IFunctionRunner, FunctionRunner>();
  }

}