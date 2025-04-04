using System.Threading.Channels;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  Task Notify(LifecycleStage stage, List<ObjectName> objs);
}

public class InProcessChangesNotifier(List<IRunnableFunction> functions, bool parallel=true) : IChangesNotifier {

  private readonly Channel<List<ObjectChangeTrigger>> pubsub = Channel.CreateUnbounded<List<ObjectChangeTrigger>>();
  
  public bool IsEmpty => pubsub.Reader.Count == 0;
  
  public Task InitDynamicTriggers(IFunctionRunner runner) {
    var triggermap = new Dictionary<ObjectChangeTrigger, List<IRunnableFunction>>();
    functions.ForEach(func => func.Triggers().ForEach(key => {
      if (!triggermap.ContainsKey(key)) triggermap[key] = [];
      triggermap[key].Add(func);
    }));

    return Task.Run(async () => {
      while (await pubsub.Reader.WaitToReadAsync()) {

        // invert the `triggermap` and get a list of functions with their triggers
        Dictionary<IRunnableFunction, List<FunctionTrigger>> allfuncs = [];
        while (pubsub.Reader.TryRead(out var triggers)) {
          triggers.ForEach(trigger => {
            if (!triggermap.TryGetValue(trigger, out var funcs)) return;
            funcs.ForEach(func => {
              if (!allfuncs.ContainsKey(func)) allfuncs[func] = [];
              allfuncs[func].Add(trigger);
            });
          });
        }
        
        // run the functions, passing list of triggers which affected the function
        var tasks = allfuncs.Keys.Select(async f => {
          DataFlowLogger.Log($"Func-To-Func Triggers[{String.Join(", ", allfuncs[f])}]", String.Empty, f.GetType().Name, [String.Empty]);
          return await runner.RunFunction(f, allfuncs[f]);
        });
        await RunTasks(tasks);
      }
    });
  }
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) {
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(obj, stage)).ToList();
    await pubsub.Writer.WriteAsync(triggers);
  }
  
  private async Task RunTasks(IEnumerable<Task> tasks) {
    if (parallel) await Task.WhenAll(tasks);
    else await tasks.Synchronous();
  }

}