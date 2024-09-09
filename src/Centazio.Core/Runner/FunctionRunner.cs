using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.Runner;

public class FunctionRunner<T, R>(
    IFunction<T, R> func, 
    IOperationRunner<T, R> oprunner, 
    ICtlRepository ctl, 
    int maxminutes = 30) 
        where T : OperationConfig
        where R : IOperationResult {

  public async Task<FunctionRunResults<R>> RunFunction() {
    var start = UtcDate.UtcNow;
    
    Log.Information("function started {@System} {@Stage}", func.Config.System, func.Config.Stage);
    
    var state = await ctl.GetOrCreateSystemState(func.Config.System, func.Config.Stage);
    if (!state.Active) {
      Log.Information("function is inactive, ignoring run {@SystemState}", state);
      return new FunctionRunResults<R>("inactive", Array.Empty<R>());
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      if (minutes <= maxminutes) {
        Log.Information("function is not idle, ignoring run {@SystemState}", state);
        return new FunctionRunResults<R>("not idle", Array.Empty<R>());
      }
      Log.Information("function considered stuck, running {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart}", state, maxminutes, minutes);
    }
    
    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state with { Status = ESystemStateStatus.Running, DateUpdated = UtcDate.UtcNow  });
      var results = await func.RunOperation(start, oprunner, ctl);
      await SaveCompletedState();
      return new FunctionRunResults<R>("success", results);
    } catch (Exception ex) {
      await SaveCompletedState();
      Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState}", state);
      return new FunctionRunResults<R>($"error: {ex.Message}", Array.Empty<R>());
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state with { Status = ESystemStateStatus.Idle, LastStarted = start, LastCompleted = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow });
  }
}

public record FunctionRunResults<R>(string Message, IEnumerable<R> OpResults) where R : IOperationResult; 