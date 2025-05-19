using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class AwsHostCentazioEngineAdapter(List<string> environments) : CentazioEngine(environments) {
  
  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo: this should support function-to-function triggers
    registrar.Register<IFunctionRunner, FunctionRunner>();
  }

}