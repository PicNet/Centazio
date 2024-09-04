using System.Diagnostics;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace centazio.core.Runner;

public class FunctionRunner<T>(IFunction func, FunctionConfig<T> cfg, ICtlRepository ctl, int maxminutes = 30) where T : OperationConfig {

  public async Task<string> RunFunction() {
    var start = UtcDate.UtcNow;
    
    Log.Information("function started {@System} {@Stage}", cfg.System, cfg.Stage);
    
    var state = await ctl.GetOrCreateSystemState(cfg.System, cfg.Stage);
    if (!state.Active) {
      Log.Information("function is inactive {@SystemState}", state);
      return $"function [{state.System.Value}/{state.Stage.Value}] inactive";
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      if (minutes <= maxminutes) {
        Log.Information("function is not idle {@SystemState}", state);
        return $"function [{state.System.Value}/{state.Stage.Value}] is not idle";
      }
      Log.Information("function considered stuck after, activating {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart}", state, maxminutes, minutes);
    }
    state = await ctl.SaveSystemState(state with { Status = ESystemStateStatus.Running, LastStarted = start, DateUpdated = UtcDate.UtcNow  });
    
    try { 
      var results = await func.Run(state, start);
      state = await SaveCompletedState();
      
      return CombineSummaryResults(state, results);
    } catch (Exception ex) {
      state = await SaveCompletedState();
      
      Log.Error(ex, "function encoutered error {@SystemState}", state);
      return $"function [{state.System.Value}/{state.Stage.Value}] encoutered error: {ex}";
    }

    async Task<SystemState> SaveCompletedState() => await ctl.SaveSystemState(state with { Status = ESystemStateStatus.Idle, LastCompleted = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow }); 
  }
  
  private string CombineSummaryResults(SystemState state, IEnumerable<OperationResult> results) {
    var message = String.Join('\n', results.Select(r => r.Message))
        .IfNullOrWhitespace($"function [{state.System.Value}/{state.Stage.Value}] completed with empty results");
    var took = state.LastCompleted - state.LastStarted ?? throw new UnreachableException(); 
    Log.Information(
        "function completed {@SystemState} {Took:N0}ms {Message}",
        state,
        took.TotalMilliseconds,
        String.IsNullOrWhiteSpace(message) ? "n/a" : message);
    return message;
  }

}