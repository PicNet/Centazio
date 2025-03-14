using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;

namespace Centazio.Core.Runner;

public interface IChangesNotifier {
  Task Notify(List<ObjectName> changes);
}

public interface IFunctionRunner {
  Task<FunctionRunResults> RunFunction<C>(AbstractFunction<C> func) where C : OperationConfig;
}

public class FunctionRunner(IChangesNotifier changesnotif, ICtlRepository ctl, CentazioSettings settings) : IFunctionRunner {
  
  public async Task<FunctionRunResults> RunFunction<C>(AbstractFunction<C> func) where C : OperationConfig {
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

    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state.Running());
      var results = await func.RunFunctionOperations(state);
      await SaveCompletedState();
      await NotifyChanges(results.Where(r => r.Result.ChangedCount > 0).ToList());
      return new SuccessFunctionRunResults(results);
    } catch (Exception ex) {
      await SaveCompletedState();
      Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState}", state);
      if (func.Config.ThrowExceptions) throw;
      // todo: need to call `NotifyChanges` for partially successfull functions
      return new ErrorFunctionRunResults(ex);
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state.Completed(func.FunctionStartTime));
  }
  
  private async Task NotifyChanges(List<OpResultAndObject> changes) {
    if (!changes.Any()) return;
    await changesnotif.Notify(changes.Select(c => c.Object).Distinct().ToList()); 
  }
}

public abstract record FunctionRunResults(List<OpResultAndObject> OpResults, string Message); 
internal sealed record SuccessFunctionRunResults(List<OpResultAndObject> OpResults) : FunctionRunResults(OpResults, "SuccessFunctionRunResults");
internal sealed record AlreadyRunningFunctionRunResults() : FunctionRunResults([], "AlreadyRunningFunctionRunResults");
internal sealed record InactiveFunctionRunResults() : FunctionRunResults([], "InactiveFunctionRunResults");
internal sealed record ErrorFunctionRunResults(Exception Exception) : FunctionRunResults([], Exception.ToString());