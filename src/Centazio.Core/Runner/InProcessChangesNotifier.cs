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
            var totrigger = NotifierUtils.GetFunctionToTriggersPairs(triggers, functions);
            await totrigger.Select(pair => runner.RunFunction(pair.Key, pair.Value)).Synchronous();
          }
        }
        finally { Running = false; }
      }
    });
  }
  
  public Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    if (!objs.Any()) return Task.CompletedTask;
    
    Running = true;
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();
    await pubsub.Writer.WriteAsync(triggers); // returns immediately (not async for unbounded channels)
    return Task.CompletedTask;
  }

  public void Dispose() {
    pubsub.Writer.Complete();
  }

}