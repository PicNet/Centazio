using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Hosts.Az;

public class AzHostCentazioEngineAdapter(List<string> environments) : CentazioEngine(environments, TimeProvider.System) {

  private readonly List<string> environments = environments;

  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    registrar.Register(provider => {
      var factory = provider.GetRequiredService<IServiceFactory<ISecretsLoader>>();
      var loader = factory.GetService();
      var secrets = loader.Load<CentazioSecrets>(environments).Result;
      return secrets;
    });
    
    registrar.Register<IFunctionRunner>(prov => new FunctionRunner(prov.GetRequiredService<ICtlRepository>(), prov.GetRequiredService<CentazioSettings>(), prov.GetRequiredService<TimeProvider>(), false));
  }

}