using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;
using Serilog;

namespace Centazio.Core.Runner;

public class FunctionRunner(IFunction func, BaseFunctionConfig cfg, ICtlRepository ctl) {

  public async Task<string> RunFunction() {
    var start = UtcDate.UtcNow;
    
    Log.Information("function started {@System} {@Stage}", cfg.System, cfg.Stage);
    cfg.Validate();
    
    var state = await ctl.GetOrCreateSystemState(cfg.System, cfg.Stage);
    if (!state.Active) {
      Log.Information("function is inactive {@SystemState}", state);
      return $"function [{state.System.Value}/{state.Stage.Value}] inactive";
    }
    if (state.Status != ESystemStateStatus.Idle) {
      var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
      // todo: make configureable?
      if (minutes <= 30) {
        Log.Information("function is not idle {@SystemState}", state);
        return $"function [{state.System.Value}/{state.Stage.Value}] is not idle";
      }
      Log.Information("function stuck running, activating {@SystemState} {@MinutesSinceStart}", state, minutes);
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
  
  private string CombineSummaryResults(SystemState state, IEnumerable<BaseFunctionOperationResult> results) {
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