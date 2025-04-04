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
      // wait for next message to pubsub queue
      while (await pubsub.Reader.WaitToReadAsync()) {
        
        Dictionary<IRunnableFunction, List<FunctionTrigger>> allfuncs = [];
        // get all waiting messages (triggers) in pubsub queue and create a map of triggers to their target function
        while (pubsub.Reader.TryRead(out var trigger)) {
          if (!triggermap.TryGetValue(trigger, out var funcs)) continue;
          funcs.ForEach(func => {
            if (!allfuncs.ContainsKey(func)) allfuncs[func] = [];
            allfuncs[func].Add(trigger);
          });
        }
        
        // run the functions, passing in which triggers affected the function
        var tasks = allfuncs.Keys.Select(async f => {
          DataFlowLogger.Log($"Func-To-Func Triggers[{String.Join(", ", allfuncs[f])}]", String.Empty, f.GetType().Name, [String.Empty]);
          return await runner.RunFunction(f, allfuncs[f]);
        });
        await RunTasks(tasks);
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