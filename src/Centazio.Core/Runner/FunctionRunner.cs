﻿using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;

namespace Centazio.Core.Runner;

public interface IFunctionRunner {
  public bool Running { get; }
  Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers);
}

public class FunctionRunner(ICtlRepository ctl, CentazioSettings settings) : IFunctionRunner {

  public bool Running { get; private set; }

  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    Running = true;
    try { return await RunImpl(func, triggers); }
    finally { Running = false; } 
  }

  private async Task<FunctionRunResults> RunImpl(IRunnableFunction func, List<FunctionTrigger> triggers) {
    var start = UtcDate.UtcNow;
    if (func.Running) {
      Log.Debug("function is already running, ignoring run {@Triggers}", triggers);
      return new AlreadyRunningFunctionRunResults();
    }
    
    // Log.Debug("checking function [{@System}/{@Stage}] - {@Now}", System, Stage, UtcDate.UtcNow);

    var state = await ctl.GetOrCreateSystemState(func.System, func.Stage);
    if (!state.Active) {
      Log.Debug("function is inactive, ignoring run {@SystemState} {@Triggers}", state, triggers);
      return new InactiveFunctionRunResults();
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
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
    
    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state.Running());
      await func.RunFunctionOperations(state, triggers, runningresults);
      return new SuccessFunctionRunResults(runningresults);
    } catch (Exception ex) {
      Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState} {@Trigger}", state, triggers);
      if (func.Config.ThrowExceptions) throw;
      return new ErrorFunctionRunResults(runningresults, ex);
    } finally {
      await SaveCompletedState();
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state.Completed(start));
  }

}

public abstract record FunctionRunResults(List<OpResultAndObject> OpResults, string Message); 
internal record SuccessFunctionRunResults(List<OpResultAndObject> OpResults) : FunctionRunResults(OpResults, "SuccessFunctionRunResults");
internal record AlreadyRunningFunctionRunResults() : FunctionRunResults([], "AlreadyRunningFunctionRunResults");
internal record InactiveFunctionRunResults() : FunctionRunResults([], "InactiveFunctionRunResults");
internal record ErrorFunctionRunResults(List<OpResultAndObject> OpResults, Exception Exception) : FunctionRunResults(OpResults, Exception.ToString());