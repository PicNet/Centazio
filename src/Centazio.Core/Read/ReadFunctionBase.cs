using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Helpers;
using Serilog;

namespace Centazio.Core.Func;

public class ReadFunctionBase(
    ReadFunctionConfig cfg,
    ICtlRepository ctl,
    IReadOperationRunner runner,
    IReadOperationsFilterAndPrioritiser? prioritiser = null) {
  
  private IReadOperationsFilterAndPrioritiser Prioritiser { get; } = prioritiser ?? new DefaultReadOperationsFilterAndPrioritiser();
  
  public async Task<string> Run() {
    cfg.Validate();
    
    var sysstate = await ctl.GetOrCreateSystemState(cfg.System, cfg.Stage);
    if (!sysstate.Active) return $"System {sysstate} is innactive.  ReadFunctionComposer not running.";
    
    var ops = (await cfg.Operations
        .LoadOperationsStates(sysstate, ctl))
        .GetReadyOperations(UtcDate.Utc.Now)
        .Prioritise(Prioritiser);
        
    var results = await RunOperationsTillAbort(UtcDate.Utc.Now, ops);
    return CombineSummaryResults(results);
  }
  
  internal async Task<IEnumerable<ReadOperationResult>> RunOperationsTillAbort(DateTime start, IEnumerable<ReadOperationStateAndConfig> ops) 
      => await ops
          .Select(op => runner.RunOperation(start, op))
          .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

  private string CombineSummaryResults(IEnumerable<ReadOperationResult> results) {
    var message = String.Join(';', results.Select(r => r.ToString()));
    Log.Information("Read Function for system {@System} completed: {message}", cfg.System, message);
    return message;
  }
}

internal static class ReadFunctionBaseHelperExtensions {
  internal static async Task<List<ReadOperationStateAndConfig>> LoadOperationsStates(this ICollection<ReadOperationConfig> ops, SystemState system, ICtlRepository ctl) {
    return (await Task.WhenAll(ops.Select(async op => new ReadOperationStateAndConfig(await ctl.GetOrCreateObjectState(system, op.Object), op))))
        .Where(op => op.State.Active)
        .ToList();
  }

  internal static IEnumerable<ReadOperationStateAndConfig> GetReadyOperations(this IEnumerable<ReadOperationStateAndConfig> states, DateTime now) {
    bool IsOperationReady(ReadOperationStateAndConfig op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.UtcNow.AddYears(-10));
      return next <= now;
    }
    return states.Where(IsOperationReady);
  }
  
  internal static IEnumerable<ReadOperationStateAndConfig> Prioritise(this IEnumerable<ReadOperationStateAndConfig> states, IReadOperationsFilterAndPrioritiser prioritiser) 
      => prioritiser.Prioritise(states); 
}