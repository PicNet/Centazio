using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
using Serilog;

namespace centazio.core.Runner;

public abstract class FunctionBase(
    ICtlRepository ctl,
    FunctionConfig cfg,
    IOperationRunner runner,
    IOperationsFilterAndPrioritiser? prioritiser = null) : IFunction {
  
  private IOperationsFilterAndPrioritiser Prioritiser { get; } = prioritiser ?? new DefaultOperationsFilterAndPrioritiser();
  
  public async Task<IEnumerable<OperationResult>> Run(SystemState state, DateTime start) => 
      await (await cfg.Operations
          .LoadOperationsStates(state, ctl))
          .GetReadyOperations(start)
          .Prioritise(Prioritiser)
          .RunOperationsTillAbort(runner, ctl, start);
}

internal static class FunctionBaseHelperExtensions {
  internal static async Task<IReadOnlyList<OperationStateAndConfig>> LoadOperationsStates(this ValidList<OperationConfig> ops, SystemState system, ICtlRepository ctl) {
    return (await Task.WhenAll(ops.Value.Select(async op => new OperationStateAndConfig(await ctl.GetOrCreateObjectState(system, op.Object), op))))
        .Where(op => op.State.Active)
        .ToList();
  }

  internal static IEnumerable<OperationStateAndConfig> GetReadyOperations(this IEnumerable<OperationStateAndConfig> states, DateTime now) {
    bool IsOperationReady(OperationStateAndConfig op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? now.AddYears(-10));
      return next <= now;
    }
    return states.Where(IsOperationReady);
  }
  
  internal static IEnumerable<OperationStateAndConfig> Prioritise(this IEnumerable<OperationStateAndConfig> states, IOperationsFilterAndPrioritiser prioritiser) 
      => prioritiser.Prioritise(states); 
  
  internal static async Task<IEnumerable<OperationResult>> RunOperationsTillAbort(this IEnumerable<OperationStateAndConfig> ops, IOperationRunner runner, ICtlRepository ctl, DateTime start) {
    return await ops
        .Select(async op => {
          try { return await SaveOperationResult(op, await runner.RunOperation(start, op)); }
          catch (Exception e) { return await SaveOperationError(op, e); }
        })
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

    async Task<OperationResult> SaveOperationResult(OperationStateAndConfig op, OperationResult res) {
      var newstate = res.UpdateObjectState(op.State, start);
      await ctl.SaveObjectState(newstate);
      Log.Information("read operation completed {@Operation} {@Results} {@UpdatedObjectState}", op, res, newstate);
      return res;
    }

    async Task<OperationResult> SaveOperationError(OperationStateAndConfig op, Exception ex) {
      var res = new EmptyOperationResult(EOperationResult.Error, ex.Message, EOperationAbortVote.Abort, ex);
      var newstate = res.UpdateObjectState(op.State, start);
      await ctl.SaveObjectState(newstate);
      Log.Information("read operation completed {@Operation} {@Results} {@UpdatedObjectState}", op, res, newstate);
      return res;
    }
    
     
  }

}