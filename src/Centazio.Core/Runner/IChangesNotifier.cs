namespace Centazio.Core.Runner;

// todo CP: consider Setup and Init usages, init is using for the in-process notifier but setup using for the cloud notifier 
public interface IChangesNotifier {
  void Init(List<IRunnableFunction> functions);
  Task Run(IFunctionRunner runner);
  Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs);
  // todo CP: what is this Setup method and why does it have to be called on every function run?
  Task Setup(IRunnableFunction func);

}

public static class NotifierUtils {
  public static Dictionary<IRunnableFunction, List<FunctionTrigger>> GetFunctionToTriggersPairs(List<ObjectChangeTrigger> triggers, List<IRunnableFunction> functions) {
    var totrigger = new Dictionary<IRunnableFunction, List<FunctionTrigger>>();
    triggers.ForEach(trigger => {
      var funcs = functions.Where(func => func.IsTriggeredBy(trigger)).ToList();
      funcs.ForEach(func => {
        if (!totrigger.ContainsKey(func)) totrigger[func] = [];
        totrigger[func].Add(trigger);
      });
    });
    return totrigger;
  }
}