using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  Task Notify(LifecycleStage stage, List<ObjectName> objs);
}

public interface IFunctionRunner {
  Task<FunctionRunResults> RunFunction(IRunnableFunction func);
}

public class FunctionRunner(IChangesNotifier changesnotif, ICtlRepository ctl, CentazioSettings settings) : IFunctionRunner {
  
  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func) {
    var start = UtcDate.UtcNow;
    if (func.Running) return new AlreadyRunningFunctionRunResults();

    // Log.Debug("checking function [{@System}/{@Stage}] - {@Now}", System, Stage, UtcDate.UtcNow);

    var state = await ctl.GetOrCreateSystemState(func.System, func.Stage);
    if (!state.Active) {
      Log.Debug("function is inactive, ignoring run {@SystemState}", state);
      return new InactiveFunctionRunResults();
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      var maxallowed = func.Config.TimeoutMinutes > 0 ? func.Config.TimeoutMinutes : settings.Defaults.FunctionMaxAllowedRunningMinutes; 
      if (minutes <= maxallowed) {
        Log.Debug("function is already running, ignoring run {@SystemState}", state);
        return new AlreadyRunningFunctionRunResults();
      }
      Log.Information("function considered stuck, running again {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart}", state, func.Config.TimeoutMinutes, minutes);
    }

    // keep track of running results, so even if one operation fails, we can
    //    notify other functions of successfull operations
    List<OpResultAndObject> runningresults = [];
    
    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state.Running());
      await func.RunFunctionOperations(state, runningresults);
      return new SuccessFunctionRunResults(runningresults);
    } catch (Exception ex) {
      Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState}", state);
      if (func.Config.ThrowExceptions) throw;
      return new ErrorFunctionRunResults(ex);
    } finally {
      await SaveCompletedState();
      await NotifyChanges(func.Stage, runningresults);
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state.Completed(start));
    
    async Task NotifyChanges(LifecycleStage stage, List<OpResultAndObject> changes) {
      var wcounts = changes.Where(r => r.Result.ChangedCount > 0).ToList();
      if (!wcounts.Any()) return;
      await changesnotif.Notify(stage, wcounts.Select(c => c.Object).Distinct().ToList());
    }
  }
}

public abstract record FunctionRunResults(List<OpResultAndObject> OpResults, string Message); 
internal sealed record SuccessFunctionRunResults(List<OpResultAndObject> OpResults) : FunctionRunResults(OpResults, "SuccessFunctionRunResults");
internal sealed record AlreadyRunningFunctionRunResults() : FunctionRunResults([], "AlreadyRunningFunctionRunResults");
internal sealed record InactiveFunctionRunResults() : FunctionRunResults([], "InactiveFunctionRunResults");
internal sealed record ErrorFunctionRunResults(Exception Exception) : FunctionRunResults([], Exception.ToString());