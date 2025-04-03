using System.Threading.Channels;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  Task Notify(LifecycleStage stage, List<ObjectName> objs);
}

public class InProcessChangesNotifier(List<IRunnableFunction> functions, bool parallel=true) : IChangesNotifier {

  private readonly Channel<ObjectChangeTrigger> pubsub = Channel.CreateUnbounded<ObjectChangeTrigger>();
  
  public bool IsEmpty => pubsub.Reader.Count == 0;
  
  public Task InitDynamicTriggers(IFunctionRunner runner) {
    var triggermap = new Dictionary<ObjectChangeTrigger, List<IRunnableFunction>>();
    functions.ForEach(func => func.Triggers().ForEach(key => {
      if (!triggermap.ContainsKey(key)) triggermap[key] = [];
      triggermap[key].Add(func);
    }));

    return Task.Run(async () => {
      while (await pubsub.Reader.WaitToReadAsync()) {
        while (pubsub.Reader.TryRead(out var trigger)) {
          if (!triggermap.TryGetValue(trigger, out var funcs)) { break; }
          var tasks = funcs.Select(async f => {
            DataFlowLogger.Log($"Func-To-Func Trigger[{trigger.Object}]", trigger.Stage, f.GetType().Name, [trigger.Object]);
            return await runner.RunFunction(f, trigger);
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