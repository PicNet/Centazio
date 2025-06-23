namespace Centazio.Core.Runner;

public class FunctionRunnerWithNotificationAdapter(IFunctionRunner runner, IChangesNotifier notifier) : IFunctionRunner {
  
  private int trackerid;
  private bool running;
  public bool Running => running || notifier.Running || runner.Running;
  
  public event EventHandler<FunctionRunningEventArgs>? OnFunctionRunning;
  
  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    var thisid = ++trackerid;
    running = true;
    await notifier.Setup(func);

    OnFunctionRunning?.Invoke(this, new FunctionRunningEventArgs(func.GetType(), triggers));
  
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
  
  public class FunctionRunningEventArgs(Type func, List<FunctionTrigger> triggers) : EventArgs {
    public Type FunctionType { get; private set; } = func;
    public IReadOnlyList<FunctionTrigger> Triggers { get; private set; } = triggers.AsReadOnly();
  }
}
