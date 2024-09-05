using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.Runner;

public class FunctionRunner<T>(IFunction func, FunctionConfig<T> cfg, ICtlRepository ctl, int maxminutes = 30) where T : OperationConfig {

  public async Task<FunctionRunResults> RunFunction() {
    var start = UtcDate.UtcNow;
    
    Log.Information("function started {@System} {@Stage}", cfg.System, cfg.Stage);
    
    var state = await ctl.GetOrCreateSystemState(cfg.System, cfg.Stage);
    if (!state.Active) {
      Log.Information("function is inactive, ignoring run {@SystemState}", state);
      return new FunctionRunResults("inactive", Array.Empty<OperationResult>());
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      if (minutes <= maxminutes) {
        Log.Information("function is not idle, ignoring run {@SystemState}", state);
        return new FunctionRunResults("not idle", Array.Empty<OperationResult>());
      }
      Log.Information("function considered stuck, running {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart}", state, maxminutes, minutes);
    }
    
    try {
      // not setting last start here as we need the LastStart to represent the time the function was started before this run
      state = await ctl.SaveSystemState(state with { Status = ESystemStateStatus.Running, DateUpdated = UtcDate.UtcNow  });
      var results = await func.Run(start);
      await SaveCompletedState();
      return new FunctionRunResults("success", results);
    } catch (Exception ex) {
      await SaveCompletedState();
      Log.Error(ex, "function encoutered error {@SystemState}", state);
      return new FunctionRunResults($"error: {ex.Message}", Array.Empty<OperationResult>());
    }

    async Task SaveCompletedState() => await ctl.SaveSystemState(state with { Status = ESystemStateStatus.Idle, LastStarted = start, LastCompleted = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow });
  }
}

public record FunctionRunResults(string Message, IEnumerable<OperationResult> OpResults); 