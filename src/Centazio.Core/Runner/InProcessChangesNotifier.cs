using System.Threading.Channels;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  Task Notify(LifecycleStage stage, List<ObjectName> objs);
}

public class InProcessChangesNotifier(List<IRunnableFunction> functions, bool parallel=true) : IChangesNotifier {

  private readonly Channel<OpChangeTriggerKey> pubsub = Channel.CreateUnbounded<OpChangeTriggerKey>();
  
  public bool IsEmpty => pubsub.Reader.Count == 0;
  
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
          var tasks = pubs.Select(async f => {
            DataFlowLogger.Log($"Func-To-Func Trigger[{key.Object}]", key.Stage, f.GetType().Name, [key.Object]);
            return await runner.RunFunction(f);
          });
          await RunTasks(tasks);
        }
      }
    });
  }
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) {
    var tasks = objs.Select(async obj => await pubsub.Writer.WriteAsync(new(obj, stage)));
    await RunTasks(tasks);
  }
  
  private async Task RunTasks(IEnumerable<Task> tasks) {
    if (parallel) await Task.WhenAll(tasks);
    else await tasks.Synchronous();
  }

}