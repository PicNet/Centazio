using System.Threading.Channels;

namespace Centazio.Core.Runner;

public class InProcessChangesNotifier : IChangesNotifier, IDisposable {

  private readonly Channel<List<ObjectChangeTrigger>> pubsub = Channel.CreateUnbounded<List<ObjectChangeTrigger>>();
  private List<IRunnableFunction> functions = null!;
  
  public bool IsAsync => true;
  
  public void Init(List<IRunnableFunction> funcs) { functions = funcs; }
  
  public Task Run(IFunctionRunner runner) {
    return Task.Run(async () => {
      while (await pubsub.Reader.WaitToReadAsync()) {
        while (pubsub.Reader.TryRead(out var triggers)) {
          var totrigger = NotifierUtils.GetFunctionToTriggersPairs(triggers, functions);
          foreach (var pair in totrigger) { await runner.RunFunction(pair.Key, pair.Value); }
        }
      }
    });
  }
  
  public async Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();
    await pubsub.Writer.WriteAsync(triggers);
  }

  public void Dispose() {
    pubsub.Writer.Complete();
  }

}