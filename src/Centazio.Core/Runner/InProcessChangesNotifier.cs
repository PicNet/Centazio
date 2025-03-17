using System.Threading.Channels;

namespace Centazio.Core.Runner;

public class InProcessChangesNotifier(List<IRunnableFunction> functions) : IChangesNotifier {

  internal readonly Channel<OpChangeTriggerKey> pubsub = Channel.CreateUnbounded<OpChangeTriggerKey>();
  
  public Task InitDynamicTriggers(IFunctionRunner runner) {
    var triggermap = new Dictionary<OpChangeTriggerKey, List<IRunnableFunction>>();
    functions.ForEach(func => func.Triggers().ForEach(key => {
      if (!triggermap.ContainsKey(key)) triggermap[key] = [];
      triggermap[key].Add(func);
    }));
    
    return Task.Run(async () => {
      while (await pubsub.Reader.WaitToReadAsync()) {
        while (pubsub.Reader.TryRead(out var key)) {
          if (!triggermap.TryGetValue(key, out var pubs)) return;
          await pubs.Select(async f => {
            DataFlowLogger.Log($"Func-To-Func Trigger[{key.Object}]", key.Stage, f.GetType().Name, [key.Object]);
            return await runner.RunFunction(f);
          }).Synchronous();
        }

	    	// todo: remove
        Console.WriteLine("channel should be empty");
      }
    });
  }
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) => 
      await Task.WhenAll(objs.Select(async obj => await pubsub.Writer.WriteAsync(new (obj, stage))));

}