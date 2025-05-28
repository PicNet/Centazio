using Centazio.Core;
using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Hosts.Az;

public class AzHostCentazioEngineAdapter(List<string> environments) : CentazioEngine(environments) {

  private List<string> environments = environments;
  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo: this should support function-to-function triggers
    var factory = registrar.BuildServiceProvider().GetRequiredService<IServiceFactory<ISecretsLoader>>();
    var loader = factory.GetService();
    var secrets = loader.Load<CentazioSecrets>(environments).Result;
        
    // Register services
    registrar.Register(secrets);
    registrar.Register<ISecretsLoader>(_ => loader);
    registrar.Register<IFunctionRunner, FunctionRunner>();
  }

}