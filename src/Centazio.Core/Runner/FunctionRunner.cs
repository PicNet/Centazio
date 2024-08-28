using System.Diagnostics;
using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Func;
using Serilog;

namespace Centazio.Core.Runner;

public class FunctionRunner(IFunction func, BaseFunctionConfig cfg, ICtlRepository ctl) {

  public async Task<string> RunFunction() {
    var start = UtcDate.UtcNow;
    
    Log.Information("RunFunction [{System}/{Stage}] started [{Start:o}]", cfg.System.Value, cfg.Stage.Value, start);
    cfg.Validate();
    
    var state = await ctl.GetOrCreateSystemState(cfg.System, cfg.Stage);
    if (!state.Active) {
      Log.Information($"System {state} is inactive.  ReadFunctionComposer not running.");
      return $"Function [{state.System.Value}/{state.Stage.Value}] inactive";
    }
    var results = await func.Run(state, start);
    
    state = state with { LastStarted = start, LastCompleted = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow };
    await ctl.SaveSystemState(state);
    
    return CombineSummaryResults(state, results);
  }
  
  private string CombineSummaryResults(SystemState state, IEnumerable<BaseFunctionOperationResult> results) {
    var message = String.Join('\n', results.Select(r => r.Message))
        .IfNullOrWhitespace($"Function [{state.System.Value}/{state.Stage.Value}] completed with empty results");
    var took = state.LastCompleted - state.LastStarted ?? throw new UnreachableException(); 
    Log.Information(
        "{Stage} Function for system {System}  end: {LastCompleted:o} took: {Took:N0}ms\n\t{Message}",
        state.Stage.Value,
        state.System.Value, 
        state.LastCompleted,
        took.TotalMilliseconds,
        String.IsNullOrWhiteSpace(message) ? "n/a" : message);
    return message;
  }

}