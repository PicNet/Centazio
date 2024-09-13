using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
using Serilog;

namespace Centazio.Core.Runner;

public abstract class AbstractFunction<T, R>(IOperationsFilterAndPrioritiser<T>? prioritiser = null) : IFunction<T, R> 
    where T : OperationConfig
    where R : OperationResult {
  
  private IOperationsFilterAndPrioritiser<T> Prioritiser { get; } = prioritiser ?? new DefaultOperationsFilterAndPrioritiser<T>();
  
  public abstract FunctionConfig<T> Config { get; }

  public async Task<IEnumerable<R>> RunOperation(
      IOperationRunner<T, R> runner,
      ICtlRepository ctl) {
    var sys = await ctl.GetOrCreateSystemState(Config.System, Config.Stage);
    var states = await LoadOperationsStates(Config.Operations, sys, ctl);
    var ready = GetReadyOperations(states);
    var priotised = Prioritiser.Prioritise(ready);
    var results = await RunOperationsTillAbort(priotised, runner, ctl);
    return results;
  }

  internal static async Task<IReadOnlyList<OperationStateAndConfig<T>>> LoadOperationsStates(ValidList<T> ops, SystemState system, ICtlRepository ctl) {
    return (await Task.WhenAll(ops.Value.Select(async op => new OperationStateAndConfig<T>(await ctl.GetOrCreateObjectState(system, op.Object), op))))
        .Where(op => op.State.Active)
        .ToList();
  }

  internal static IEnumerable<OperationStateAndConfig<T>> GetReadyOperations(IEnumerable<OperationStateAndConfig<T>> states) {
    bool IsOperationReady(OperationStateAndConfig<T> op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.MinValue.ToUniversalTime());
      return next <= UtcDate.UtcNow;
    }
    return states.Where(IsOperationReady);
  }
  
  internal static async Task<IEnumerable<R>> RunOperationsTillAbort(IEnumerable<OperationStateAndConfig<T>> ops, IOperationRunner<T, R> runner, ICtlRepository ctl) {
    return await ops
        .Select(async op => await RunAndSaveOp(op))
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

    async Task<R> RunAndSaveOp(OperationStateAndConfig<T> op) {
      var opstart = UtcDate.UtcNow;
      Log.Information("operation starting {@Operation}", op);
      
      var result = await RunOp(op);
      var saved = await SaveOp(op, opstart, result);
      
      Log.Information("operation completed {@Operation} {@Results} {@UpdatedObjectState} {@Took:0}ms", op, result, saved, (UtcDate.UtcNow - opstart).TotalMilliseconds);
      
      return result;
    }
    
    async Task<R> RunOp(OperationStateAndConfig<T> op) {
      try { return await runner.RunOperation(op); } 
      catch (Exception ex) { return runner.BuildErrorResult(op, ex); }
    }

    async Task<ObjectState> SaveOp(OperationStateAndConfig<T> op, DateTime start, R res) {
      var now = UtcDate.UtcNow;
      var newstate = op.State with {
        LastStart = start,
        LastCompleted = now,
        LastResult = res.Result,
        LastAbortVote = res.AbortVote,
        LastRunMessage = $"operation [{op.State.System}/{op.State.Stage}/{op.State.Object}] completed [{res.Result}] message: {res.Message}",
        LastPayLoadType = res.ResultType,
        LastPayLoadLength = res.ResultLength,
        LastRunException = res.Exception?.ToString()
      };
      if (res.Result == EOperationResult.Success) {
        newstate = newstate with {
          LastSuccessStart = start,
          LastSuccessCompleted = now
        };
      }
      
      await ctl.SaveObjectState(newstate);
      return newstate;
    }
  }

}