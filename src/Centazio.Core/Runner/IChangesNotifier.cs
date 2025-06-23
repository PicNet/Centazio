namespace Centazio.Core.Runner;

// todo CP: consider Setup and Init usages, init is using for the in-process notifier but setup using for the cloud notifier 
public interface IChangesNotifier {
  void Init(List<IRunnableFunction> functions);
  Task Run(IFunctionRunner runner);
  Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs);
  // todo CP: what is this Setup method and why does it have to be called on every
  //    function run?  Please get a reproduceable environment and show GT
  Task Setup(IRunnableFunction func);
}

public interface IMonitorableChangesNotifier : IChangesNotifier {
  bool Running { get; }
}

public static class NotifierUtils {
  public static List<FunctionBeingTriggered> GetFunctionsThatAreTriggeredByTriggers(List<ObjectChangeTrigger> triggers, List<IRunnableFunction> functions) {
    var totrigger = new Dictionary<IRunnableFunction, FunctionBeingTriggered>();
    triggers.ForEach(trigger => {
      var funcs = functions.Where(func => func.IsTriggeredBy(trigger)).ToList();
      funcs.ForEach(func => {
        var fbt = totrigger.TryGetValue(func, out var value) ? value : new (func, []);
        fbt.ResponsibleTriggers.Add(trigger);
        totrigger[func] = fbt;
      });
    });
    return totrigger.Values.ToList();
  }
}

public record FunctionBeingTriggered(IRunnableFunction Function, List<FunctionTrigger> ResponsibleTriggers);