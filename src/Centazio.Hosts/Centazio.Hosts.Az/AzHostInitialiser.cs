using Centazio.Core.Runner;

namespace Centazio.Hosts.Az;

public class AzHostInitialiser(List<string> environments) : AbstractHostInitialiser(environments) {

  protected override Task RegisterEnvironmentDependencies()  {
    // todo: this should support function-to-function triggers
    Registrar.Register<IFunctionRunner, FunctionRunner>();
    return Task.CompletedTask;
  }

}