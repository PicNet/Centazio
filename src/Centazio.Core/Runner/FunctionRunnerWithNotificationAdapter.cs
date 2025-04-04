﻿namespace Centazio.Core.Runner;

public class FunctionRunnerWithNotificationAdapter(IFunctionRunner runner, IChangesNotifier notifier) : IFunctionRunner {

  public bool Running => runner.Running;

  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    var results = await runner.RunFunction(func, triggers);
    var wcounts = results.OpResults.Where(r => r.Result.ChangedCount > 0).ToList();
    if (!wcounts.Any()) return results;
    await notifier.Notify(func.Stage, wcounts.Select(c => c.Object).Distinct().ToList());
    return results;
  }

}