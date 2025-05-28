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
  private List<string> environments = environments;
  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo: this should support function-to-function triggers
    var notifier = new AwsSqsChangesNotifier(localaws);
    var factory = registrar.BuildServiceProvider().GetRequiredService<IServiceFactory<ISecretsLoader>>();
    var loader = factory.GetService();
    var secrets = loader.Load<CentazioSecrets>(environments).Result;
    
    registrar.Register(secrets);
    registrar.Register<ISecretsLoader>(_ => loader);
    registrar.Register<IChangesNotifier>(notifier);
    registrar.Register<IFunctionRunner>(prov => {
      var inner = new FunctionRunner(prov.GetRequiredService<ICtlRepository>(), settings);
      return new FunctionRunnerWithNotificationAdapter(inner, notifier, () => {});
    });

  }
}