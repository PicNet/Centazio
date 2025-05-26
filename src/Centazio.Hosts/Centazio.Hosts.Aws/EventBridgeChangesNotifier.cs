using Centazio.Core;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class EventBridgeChangesNotifier : IChangesNotifier, IDisposable {

  public void Init(List<IRunnableFunction> functions) {
    // TODO setup the event bridge
    // throw new NotImplementedException();
  }

  public Task Run(IFunctionRunner runner) {
    // TODO check if we need to implement run for event bridge
    //throw new NotImplementedException();
    return Task.CompletedTask;
  }

  public Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    // throw new NotImplementedException();
    // TODO notify to the event bridge
    return Task.CompletedTask;
  }

  public bool IsAsync => true;

  public void Dispose() { // TODO release managed resources here
  }

}