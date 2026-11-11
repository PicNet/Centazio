namespace Centazio.Core.Runner;

public class FunctionRunnerWithNotificationAdapter(IFunctionRunner runner, IChangesNotifier notifier) : IFunctionRunner {
  
  public event EventHandler<FunctionRunningEventArgs>? OnFunctionRunning;
  
  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    await notifier.Setup(func);

    OnFunctionRunning?.Invoke(this, new FunctionRunningEventArgs(func.GetType(), triggers));
  
    // the inner runner has completed and saved the function state, so dependants triggered by this
    //    notification always start after this function has finished
    var results = await runner.RunFunction(func, triggers);
    var changed = results.OpResults.Where(r => r.Result.ChangedCount > 0).Select(c => c.Object).Distinct().ToList();
    if (changed.Any()) await notifier.Notify(func.System, func.Stage, changed);
    return results;
  }
  
  public class FunctionRunningEventArgs(Type func, List<FunctionTrigger> triggers) : EventArgs {
    public Type FunctionType { get; private set; } = func;
    public IReadOnlyList<FunctionTrigger> Triggers { get; private set; } = triggers.AsReadOnly();
  }
}
