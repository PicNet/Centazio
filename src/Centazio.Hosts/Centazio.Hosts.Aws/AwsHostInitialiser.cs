using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class AwsHostInitialiser(List<string> environments) : AbstractHostInitialiser(environments) {

  protected override Task RegisterEnvironmentDependencies()  {
    // todo: this should support function-to-function triggers
    Registrar.Register<IFunctionRunner, FunctionRunner>();
    return Task.CompletedTask;
  }

}