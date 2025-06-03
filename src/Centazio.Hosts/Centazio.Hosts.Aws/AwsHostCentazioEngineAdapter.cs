using Centazio.Core.Ctl;
using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Hosts.Aws;

public class AwsHostCentazioEngineAdapter(CentazioSettings settings, List<string> environments, bool localaws) : CentazioEngine(environments) {
  private readonly List<string> environments = environments;
  private readonly Dictionary<ESecretsProviderType, Func<ISecretsLoader>> Providers = new() {
    [ESecretsProviderType.File] = () => new FileSecretsLoaderFactory(settings).GetService(),
    [ESecretsProviderType.Aws] = () => new AwsSecretsLoaderFactory(settings).GetService(),
    [ESecretsProviderType.EnvironmentVariable] = () => new EnvironmentVariableSecretsLoaderFactory().GetService()
  };
  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo CP: this should support function-to-function triggers
    
    var notifier = (IChangesNotifier)(settings.AwsSettings.EventBridge ? new AwsEventBridgeChangesNotifier() :  new AwsSqsChangesNotifier(localaws));
    var providersetting = settings.SecretsLoaderSettings.Provider;
    if (!Enum.TryParse<ESecretsProviderType>(providersetting, out var provider))
      throw new ArgumentException($"Unknown secrets provider: {providersetting}");

    if (!Providers.TryGetValue(provider, out var factory))
      throw new ArgumentException($"Provider {provider} is not implemented");

    var loader = factory();
    var secrets = loader.Load<CentazioSecrets>(environments).Result;
    
    registrar.Register(secrets);
    registrar.Register<ISecretsLoader>(_ => loader);
    registrar.Register(notifier);
    registrar.Register<IFunctionRunner>(prov => {
      var inner = new FunctionRunner(prov.GetRequiredService<ICtlRepository>(), settings);
      return new FunctionRunnerWithNotificationAdapter(inner, notifier, () => {});
    });

  }
}