using Centazio.Core;
using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Hosts.Az;

public class AzHostCentazioEngineAdapter(List<string> environments) : CentazioEngine(environments) {

  private readonly List<string> environments = environments;

  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo WT: this should support function-to-function triggers
    registrar.Register(provider => {
      var factory = provider.GetRequiredService<IServiceFactory<ISecretsLoader>>();
      var loader = factory.GetService();
      var secrets = loader.Load<CentazioSecrets>(environments).Result;
      return secrets;
    });
    
    registrar.Register<IFunctionRunner, FunctionRunner>();
  }

}