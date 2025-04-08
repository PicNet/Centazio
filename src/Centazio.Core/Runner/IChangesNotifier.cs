namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  void Init(List<IRunnableFunction> functions);
  Task Run(IFunctionRunner runner);
  Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs);
  bool IsAsync { get; }

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