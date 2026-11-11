using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;

namespace Centazio.Core.Runner;

public interface IFunctionRunner {
  Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers);
}

// the host default for functions that do not set `FunctionConfig.ThrowExceptions`.  Exceptions add value on local and test hosts, serverless hosts log them and return an error result instead
public class FunctionRunner(ICtlRepository ctl, CentazioSettings settings, TimeProvider clock, bool throws) : IFunctionRunner {

  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    var start = clock.UtcNow();
    if (func.Running) {
      Log.Debug("function is already running, ignoring run {@Triggers}", triggers);
      return new AlreadyRunningFunctionRunResults();
    }
    
    var state = await ctl.GetOrCreateSystemState(func.System, func.Stage);
    if (!state.Active) {
      Log.Debug("function is inactive, ignoring run {@SystemState} {@Triggers}", state, triggers);
      return new InactiveFunctionRunResults();
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = clock.UtcNow().Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      var maxallowed = func.Config.TimeoutMinutes > 0 ? func.Config.TimeoutMinutes : settings.Defaults.FunctionMaxAllowedRunningMinutes; 
      if (minutes <= maxallowed) {
        Log.Debug("function state is not idle, ignoring run {@SystemState} {@Triggers}", state, triggers);
        return new AlreadyRunningFunctionRunResults();
      }
      Log.Information("function considered stuck, running again {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart} {@Triggers}", state, func.Config.TimeoutMinutes, minutes, triggers);
    }

    // keep track of running results, so even if one operation fails, we can
    //    notify other functions of successfull operations
    List<OpResultAndObject> runningresults = [];
    var functhrows = func.Config.ThrowExceptions ?? throws;
    
    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state.Running(clock.UtcNow()));
      await func.RunFunctionOperations(state, triggers, runningresults, functhrows);
      return new SuccessFunctionRunResults(runningresults);
    } catch (Exception ex) {
      Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState} {@Trigger}", state, triggers);
      if (functhrows) throw;
      return new ErrorFunctionRunResults(runningresults, ex);
    } finally {
      await SaveCompletedState();
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state.Completed(start, clock.UtcNow()));
  }

}

public abstract record FunctionRunResults(List<OpResultAndObject> OpResults, string Message); 
internal record SuccessFunctionRunResults(List<OpResultAndObject> OpResults) : FunctionRunResults(OpResults, "SuccessFunctionRunResults");
internal record AlreadyRunningFunctionRunResults() : FunctionRunResults([], "AlreadyRunningFunctionRunResults");
internal record InactiveFunctionRunResults() : FunctionRunResults([], "InactiveFunctionRunResults");
internal record ErrorFunctionRunResults(List<OpResultAndObject> OpResults, Exception Exception) : FunctionRunResults(OpResults, Exception.ToString());