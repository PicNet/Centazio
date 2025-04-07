using System.Threading.Channels;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  void Init(List<IRunnableFunction> functions);
  Task Run(IFunctionRunner runner);
  Task Notify(LifecycleStage stage, List<ObjectName> objs);

}

public class InProcessChangesNotifier : IChangesNotifier {

  private readonly Channel<List<ObjectChangeTrigger>> pubsub = Channel.CreateUnbounded<List<ObjectChangeTrigger>>();
  private readonly Dictionary<ObjectChangeTrigger, List<IRunnableFunction>> triggermap = [];
  
  public void Init(List<IRunnableFunction> functions) {
    functions.ForEach(func => func.Triggers().ForEach(key => {
      if (!triggermap.ContainsKey(key)) triggermap[key] = [];
      triggermap[key].Add(func);
    }));
  }
  
  public Task Run(IFunctionRunner runner) {
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
        foreach (var func in allfuncs.Keys) {
          DataFlowLogger.Log($"Func-To-Func Triggers[{String.Join(", ", allfuncs[func])}]", String.Empty, func.GetType().Name, [String.Empty]);
          await runner.RunFunction(func, allfuncs[func]);
        }
      }
    });
  }
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) {
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(obj, stage)).ToList();
    await pubsub.Writer.WriteAsync(triggers);
  }

}