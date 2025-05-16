using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class AwsLazyFunctionInitialiser(List<string> environments, Type function) : AbstractLazyFunctionInitialiser(environments, function) {

  protected override Task RegisterEnvironmentDependencies(CentazioServicesRegistrar registrar)  {
    // todo: this should support function-to-function triggers
    registrar.Register<IFunctionRunner, FunctionRunner>();
    return Task.CompletedTask;
  }

}