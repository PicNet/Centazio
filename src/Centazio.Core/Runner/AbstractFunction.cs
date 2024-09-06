using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
using Serilog;

namespace Centazio.Core.Runner;

public abstract class AbstractFunction<T>(IOperationsFilterAndPrioritiser<T>? prioritiser = null) : IFunction<T> where T : OperationConfig {
  
  private IOperationsFilterAndPrioritiser<T> Prioritiser { get; } = prioritiser ?? new DefaultOperationsFilterAndPrioritiser<T>();
  
  public abstract FunctionConfig<T> Config { get; }

  public async Task<IEnumerable<OperationResult>> RunOperation(
      DateTime start, 
      IOperationRunner<T> runner,
      ICtlRepository ctl) {
    var sys = await ctl.GetOrCreateSystemState(Config.System, Config.Stage);
    var states = await LoadOperationsStates(Config.Operations, sys, ctl);
    var ready = GetReadyOperations(states, start);
    var priotised = Prioritiser.Prioritise(ready);
    var results = await RunOperationsTillAbort(priotised, runner, ctl, start);
    return results;
  }

  internal static async Task<IReadOnlyList<OperationStateAndConfig<T>>> LoadOperationsStates(ValidList<T> ops, SystemState system, ICtlRepository ctl) {
    return (await Task.WhenAll(ops.Value.Select(async op => new OperationStateAndConfig<T>(await ctl.GetOrCreateObjectState(system, op.Object), op))))
        .Where(op => op.State.Active)
        .ToList();
  }

  internal static IEnumerable<OperationStateAndConfig<T>> GetReadyOperations(IEnumerable<OperationStateAndConfig<T>> states, DateTime now) {
    bool IsOperationReady(OperationStateAndConfig<T> op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? now.AddYears(-10));
      return next <= now;
    }
    return states.Where(IsOperationReady);
  }
  
  internal static async Task<IEnumerable<OperationResult>> RunOperationsTillAbort(IEnumerable<OperationStateAndConfig<T>> ops, IOperationRunner<T> runner, ICtlRepository ctl, DateTime start) {
    return await ops
        .Select(async op => await RunAndSaveOp(op))
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

    async Task<OperationResult> RunAndSaveOp(OperationStateAndConfig<T> op) {
      var opstart = UtcDate.UtcNow;
      Log.Information("operation starting {@Operation}", op);
      
      var result = await RunOp(op);
      return await SaveOp(opstart, op, result);
    }
    
    async Task<OperationResult> RunOp(OperationStateAndConfig<T> op) {
      try { return await runner.RunOperation(start, op); }
      catch (Exception ex) { return OperationResult.Error(EOperationAbortVote.Abort, ex); }
    }

    async Task<OperationResult> SaveOp(DateTime opstart, OperationStateAndConfig<T> op, OperationResult res) {
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
      Log.Information("operation completed {@Operation} {@Results} {@UpdatedObjectState} {@Took:0}ms", op, res, newstate, (UtcDate.UtcNow - opstart).TotalMilliseconds);
      return res;
    }
  }

}