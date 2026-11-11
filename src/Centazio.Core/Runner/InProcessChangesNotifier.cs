using System.Threading.Channels;

namespace Centazio.Core.Runner;

/// <summary>
/// Queues change notifications and runs the functions they trigger.  `ProcessPending` drains the queue on the
/// caller's flow (deterministic, used by tests and simulations), `Run` keeps draining as notifications arrive and
/// is started in the background by the host.
/// </summary>
public class InProcessChangesNotifier : IChangesNotifier, IDisposable {

  private readonly Channel<List<ObjectChangeTrigger>> pubsub = Channel.CreateUnbounded<List<ObjectChangeTrigger>>();
  private List<IRunnableFunction> functions = null!;
  
  public Task Setup(IRunnableFunction func) => Task.CompletedTask;
  public void Init(List<IRunnableFunction> funcs) { functions = funcs; }
  
  public async Task Run(IFunctionRunner runner) {
    // returns false only when the channel is closed during Dispose
    while (await pubsub.Reader.WaitToReadAsync()) await ProcessPending(runner);
  }
  
  /// <summary>
  /// Runs every queued notification, including those queued by the functions it runs, and returns when the queue is empty 
  /// </summary>
  public async Task ProcessPending(IFunctionRunner runner) {
    while (pubsub.Reader.TryRead(out var triggers)) {
      var totrigger = NotifierUtils.GetFunctionsThatAreTriggeredByTriggers(triggers, functions);
      await totrigger.Select(func => runner.RunFunction(func.Function, func.ResponsibleTriggers)).Synchronous();
    }
  }
  
  public async Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    if (!objs.Any()) return;
    
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();
    await pubsub.Writer.WriteAsync(triggers); // returns immediately (not async for unbounded channels)
  }

  public void Dispose() => 
      pubsub.Writer.Complete();

}