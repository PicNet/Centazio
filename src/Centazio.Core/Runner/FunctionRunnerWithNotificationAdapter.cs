namespace Centazio.Core.Runner;

public class FunctionRunnerWithNotificationAdapter(IFunctionRunner runner, IChangesNotifier notifier, Action runningfunc) : IFunctionRunner {

  public bool Running => runner.Running;

  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    await notifier.Setup(func);
    
    runningfunc(); // notify that we are running a function
    
    var results = await runner.RunFunction(func, triggers);
    var wcounts = results.OpResults.Where(r => r.Result.ChangedCount > 0).ToList();
    if (!wcounts.Any()) return results;
    await notifier.Notify(func.System, func.Stage, wcounts.Select(c => c.Object).Distinct().ToList());
    return results;
  }

}