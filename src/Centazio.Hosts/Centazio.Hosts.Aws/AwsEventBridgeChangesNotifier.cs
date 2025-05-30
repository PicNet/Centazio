using Centazio.Core;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class AwsEventBridgeChangesNotifier : IChangesNotifier, IDisposable {

  public void Dispose() { // TODO release managed resources here
  }

  public void Init(List<IRunnableFunction> functions) {
    throw new NotImplementedException();
  }

  public Task Run(IFunctionRunner runner) => throw new NotImplementedException();

  public Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) => throw new NotImplementedException();
  public bool IsAsync { get; }

}