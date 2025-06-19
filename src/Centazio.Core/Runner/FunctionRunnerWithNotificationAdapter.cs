namespace Centazio.Core.Runner;

public class FunctionRunnerWithNotificationAdapter(IFunctionRunner runner, IChangesNotifier notifier, Action runningfunc) : IFunctionRunner {
  
  private bool running;
  public bool Running => running || notifier.Running || runner.Running;

  private int trackerid;
  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    var thisid = ++trackerid;
    running = true;
    await notifier.Setup(func);

    // todo GT: replace Action runningfunc with events instead?
    runningfunc(); // notify that we are running a function
  
    var results = await runner.RunFunction(func, triggers);
    var wcounts = results.OpResults.Where(r => r.Result.ChangedCount > 0).ToList();
    if (!wcounts.Any()) {
      if (thisid == trackerid) running = false;
      running = false;
      return results;
    }
    
    _ = Task.Run(async () => {
      // notify in bg thread to ensre the current running function completes before new functions start
      try {
        await Task.Delay(1);
        await notifier.Notify(func.System, func.Stage, wcounts.Select(c => c.Object).Distinct().ToList());
      } finally {
        if (thisid == trackerid) running = false;
      }
    });
    return results;
  }
}
