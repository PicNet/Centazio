using System.Threading.Channels;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  Task Notify(LifecycleStage stage, List<ObjectName> objs);
}

public class InProcessChangesNotifier(List<IRunnableFunction> functions) : IChangesNotifier {

  private readonly Channel<OpChangeTriggerKey> pubsub = Channel.CreateUnbounded<OpChangeTriggerKey>();
  
  public bool IsEmpty => pubsub.Reader.Count == 0;
  
  // todo: instead of taking the runner here should we instead take an Func<IRunnableFunction, Task>
  //    and instead of `IRunnableFunction` use a generic type that supports Triggers function?
  public Task InitDynamicTriggers(IFunctionRunner runner) {
    var triggermap = new Dictionary<OpChangeTriggerKey, List<IRunnableFunction>>();
    functions.ForEach(func => func.Triggers().ForEach(key => {
      if (!triggermap.ContainsKey(key)) triggermap[key] = [];
      triggermap[key].Add(func);
    }));

    return Task.Run(async () => {
      while (await pubsub.Reader.WaitToReadAsync()) {
        while (pubsub.Reader.TryRead(out var key)) {
          if (!triggermap.TryGetValue(key, out var pubs)) { break; }
          await pubs.Select(async f => {
            DataFlowLogger.Log($"Func-To-Func Trigger[{key.Object}]", key.Stage, f.GetType().Name, [key.Object]);
            return await runner.RunFunction(f);
          }).Synchronous(throttlemillis: 50);
          // todo: throttlemillis required so SQLite does not lock, move to parameter or somewhere better
        }
      }
    });
  }
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) => 
      await Task.WhenAll(objs.Select(async obj => await pubsub.Writer.WriteAsync(new (obj, stage))));

}