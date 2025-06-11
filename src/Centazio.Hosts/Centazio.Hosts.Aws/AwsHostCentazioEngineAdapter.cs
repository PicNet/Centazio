using Amazon.EventBridge;
using Amazon.Lambda;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Hosts.Aws;

public class AwsHostCentazioEngineAdapter(CentazioSettings settings, List<string> environments, bool localaws) : CentazioEngine(environments) {
  private readonly List<string> environments = environments;

  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    var notifier = (IChangesNotifier)(settings.AwsSettings.EventBridge ? new AwsEventBridgeChangesNotifier(new AmazonLambdaClient(), new AmazonEventBridgeClient()) :  new AwsSqsChangesNotifier(localaws));
    
    registrar.Register(provider => {
      var factory = provider.GetRequiredService<IServiceFactory<ISecretsLoader>>();
      var loader = factory.GetService();
      var secrets = loader.Load<CentazioSecrets>(environments).Result;
      return secrets;
    });
    
    registrar.Register(notifier);
    registrar.Register<IFunctionRunner>(prov => {
      var inner = new FunctionRunner(prov.GetRequiredService<ICtlRepository>(), settings);
      return new FunctionRunnerWithNotificationAdapter(inner, notifier, () => {});
    });

  }
}