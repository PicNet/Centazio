using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Func;

public abstract class ReadFunctionBase(
    ReadFunctionConfig cfg,
    ICtlRepository ctl,
    IReadOperationRunner runner,
    IReadOperationsFilterAndPrioritiser? prioritiser = null) : IFunction {
  
  private IReadOperationsFilterAndPrioritiser Prioritiser { get; } = prioritiser ?? new DefaultReadOperationsFilterAndPrioritiser();
  
  public async Task<IEnumerable<BaseFunctionOperationResult>> Run(SystemState state, DateTime start) => 
      await (await cfg.Operations
          .LoadOperationsStates(state, ctl))
          .GetReadyOperations(UtcDate.UtcNow)
          .Prioritise(Prioritiser)
          .RunOperationsTillAbort(runner, ctl, start);
}

internal static class ReadFunctionBaseHelperExtensions {
  internal static async Task<List<ReadOperationStateAndConfig>> LoadOperationsStates(this ICollection<ReadOperationConfig> ops, SystemState system, ICtlRepository ctl) {
    return (await Task.WhenAll(ops.Select(async op => new ReadOperationStateAndConfig(await ctl.GetOrCreateObjectState(system, op.Object), op))))
        .Where(op => op.State.Active)
        .ToList();
  }

  internal static IEnumerable<ReadOperationStateAndConfig> GetReadyOperations(this IEnumerable<ReadOperationStateAndConfig> states, DateTime now) {
    bool IsOperationReady(ReadOperationStateAndConfig op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? now.AddYears(-10));
      return next <= now;
    }
    return states.Where(IsOperationReady);
  }
  
  internal static IEnumerable<ReadOperationStateAndConfig> Prioritise(this IEnumerable<ReadOperationStateAndConfig> states, IReadOperationsFilterAndPrioritiser prioritiser) 
      => prioritiser.Prioritise(states); 
  
  internal static async Task<IEnumerable<ReadOperationResult>> RunOperationsTillAbort(this IEnumerable<ReadOperationStateAndConfig> ops, IReadOperationRunner runner, ICtlRepository ctl, DateTime start) {
    return await ops
        .Select(async op => {
          try { return await SaveOperationResult(op, await runner.RunOperation(start, op)); }
          catch (Exception e) { return await SaveOperationError(op, e); }
        })
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

    async Task<ReadOperationResult> SaveOperationResult(ReadOperationStateAndConfig op, ReadOperationResult res) {
      var newstate = res.UpdateObjectState(op.State, start);
      await ctl.SaveObjectState(newstate);
      Log.Information("read operation completed {@Operation} {@Results} {@UpdatedObjectState}", op, res, newstate);
      return res;
    }

    async Task<ReadOperationResult> SaveOperationError(ReadOperationStateAndConfig op, Exception ex) {
      var res = new EmptyReadOperationResult(EOperationReadResult.FailedRead, ex.Message, EOperationAbortVote.Abort, ex);
      var newstate = res.UpdateObjectState(op.State, start);
      await ctl.SaveObjectState(newstate);
      Log.Information("read operation failed {@Operation} {@Results} {@UpdatedObjectState}", op, res, newstate);
      return res;
    }
    
     
  }

}