using System.Threading.Channels;

namespace Centazio.Core.Runner;

public class InProcessChangesNotifier : IChangesNotifier, IDisposable {

  private readonly Channel<List<ObjectChangeTrigger>> pubsub = Channel.CreateUnbounded<List<ObjectChangeTrigger>>();
  private List<IRunnableFunction> functions = null!;
  
  public bool Running  { get; private set; }
  
  public Task Setup(IRunnableFunction func) => Task.CompletedTask;
  public void Init(List<IRunnableFunction> funcs) { functions = funcs; }
  
  public Task Run(IFunctionRunner runner) {
    return Task.Run(async () => {
      while (await pubsub.Reader.WaitToReadAsync()) { // returns false only when the channel is closed during Dispose
        Running = true;
        try {
          while (pubsub.Reader.TryRead(out var triggers)) { // not async, and will read all triggers in the channel
            var totrigger = NotifierUtils.GetFunctionsThatAreTriggeredByTriggers(triggers, functions);
            await totrigger.Select(func => runner.RunFunction(func.Function, func.ResponsibleTriggers)).Synchronous();
          }
        }
        finally { Running = false; }
      }
    });
  }
  
  public async Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    if (!objs.Any()) return;
    
    Running = true;
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();
    await pubsub.Writer.WriteAsync(triggers); // returns immediately (not async for unbounded channels)
  }

  public void Dispose() => 
      pubsub.Writer.Complete();

}