using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class AwsHostCentazioEngineAdapter(List<string> environments, bool localaws) : CentazioEngine(environments) {
  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    // todo: this should support function-to-function triggers
    var notifier = new AwsSqlChangesNotifier(localaws);
        
    registrar.Register<IChangesNotifier>(notifier);
    registrar.Register<IFunctionRunner, FunctionRunner>();

  }
}