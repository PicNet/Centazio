using System.Threading.Channels;

namespace Centazio.Core.Runner;

public class InProcessChangesNotifier : IChangesNotifier {

  private readonly Channel<List<ObjectChangeTrigger>> pubsub = Channel.CreateUnbounded<List<ObjectChangeTrigger>>();
  private List<IRunnableFunction> functions = null!;
  
  // todo: move to ctor if possible?
  public void Init(List<IRunnableFunction> funcs) { functions = funcs; }
  
  // todo: copy new logic into the InstantNotifier
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

}