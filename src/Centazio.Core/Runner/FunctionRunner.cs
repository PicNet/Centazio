using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.Runner;

public class FunctionRunner<C, R>(
    AbstractFunction<C, R> func, 
    IOperationRunner<C, R> oprunner, 
    ICtlRepository ctl) : IDisposable
        where C : OperationConfig
        where R : OperationResult {

  public async Task<FunctionRunResults<R>> RunFunction() {
    var start = UtcDate.UtcNow;
    
    Log.Information("function started {@System} {@Stage}", func.Config.System, func.Config.Stage);
    
    var state = await ctl.GetOrCreateSystemState(func.Config.System, func.Config.Stage);
    if (!state.Active) {
      Log.Information("function is inactive, ignoring run {@SystemState}", state);
      return new InactiveFunctionRunResults<R>();
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      if (minutes <= func.Config.TimeoutMinutes) {
        Log.Information("function is already running, ignoring run {@SystemState}", state);
        return new AlreadyRunningFunctionRunResults<R>();
      }
      Log.Information("function considered stuck, running again {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart}", state, func.Config.TimeoutMinutes, minutes);
    }
    
    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state.Running());
      var results = await func.RunFunctionOperations(oprunner, ctl);
      await SaveCompletedState();
      return new SuccessFunctionRunResults<R>(results);
    } catch (Exception ex) {
      await SaveCompletedState();
      Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState}", state);
      if (func.Config.ThrowExceptions) throw;
      return new ErrorFunctionRunResults<R>(ex);
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state.Completed(start));
  }

  public void Dispose() { func.Dispose(); }

}

public abstract record FunctionRunResults<R>(List<R> OpResults, string Message) where R : OperationResult; 
public record SuccessFunctionRunResults<R>(List<R> OpResults) : FunctionRunResults<R>(OpResults, "SuccessFunctionRunResults") where R : OperationResult;
public record AlreadyRunningFunctionRunResults<R>() : FunctionRunResults<R>([], "AlreadyRunningFunctionRunResults") where R : OperationResult;
public record InactiveFunctionRunResults<R>() : FunctionRunResults<R>([], "InactiveFunctionRunResults") where R : OperationResult;
// ReSharper disable once NotAccessedPositionalProperty.Global
public record ErrorFunctionRunResults<R>(Exception Exception) : FunctionRunResults<R>([], Exception.ToString()) where R : OperationResult;