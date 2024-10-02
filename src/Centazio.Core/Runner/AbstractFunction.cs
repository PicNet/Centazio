using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.Runner;

public abstract class AbstractFunction<C, O, R> : IDisposable  
    where C : OperationConfig<O>
    where O : ObjectName
    where R : OperationResult{
  
  public abstract FunctionConfig<C, O> Config { get; }

  public virtual async Task<List<R>> RunFunctionOperations(
      IOperationRunner<C, O, R> runner,
      ICtlRepository ctl) {
    var sys = await ctl.GetOrCreateSystemState(Config.System, Config.Stage);
    var states = await LoadOperationsStates(Config, sys, ctl);
    var ready = GetReadyOperations(states);
    var results = await RunOperationsTillAbort(ready, runner, ctl, Config.ThrowExceptions);
    return results;
  }

  internal static async Task<List<OperationStateAndConfig<C, O>>> LoadOperationsStates(FunctionConfig<C, O> conf, SystemState system, ICtlRepository ctl) {
    return (await conf.Operations
            .Select(async op => {
      var state = await ctl.GetObjectStateRepo<O>().GetOrCreateObjectState(system, op.Object);
      var checkpoint = state.LastSuccessStart ?? op.FirstTimeCheckpoint ?? conf.DefaultFirstTimeCheckpoint;
      return new OperationStateAndConfig<C, O>(state, conf, op, checkpoint);
    }).Synchronous())
    .Where(op => op.State.Active)
    .ToList();
  }

  internal static List<OperationStateAndConfig<C, O>> GetReadyOperations(List<OperationStateAndConfig<C, O>> states) {
    bool IsOperationReady(OperationStateAndConfig<C, O> op) {
      var next = op.OpConfig.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.MinValue.ToUniversalTime());
      return next <= UtcDate.UtcNow;
    }
    return states.Where(IsOperationReady).ToList();
  }
  
  internal static async Task<List<R>> RunOperationsTillAbort(List<OperationStateAndConfig<C, O>> ops, IOperationRunner<C, O, R> runner, ICtlRepository ctl, bool throws = true) {
    return await ops
        .Select(async op => await RunAndSaveOp(op))
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

    async Task<R> RunAndSaveOp(OperationStateAndConfig<C, O> op) {
      var opstart = UtcDate.UtcNow;
      Log.Information("operation starting {@Operation}", op);
      
      var result = await RunOp(op);
      var saved = await SaveOp(op, opstart, result);
      
      Log.Information("operation completed {@Operation} {@Results} {@UpdatedObjectState} {@Took:0}ms", op, result, saved, (UtcDate.UtcNow - opstart).TotalMilliseconds);
      
      return result;
    }
    
    async Task<R> RunOp(OperationStateAndConfig<C, O> op) {
      try { return await runner.RunOperation(op); } 
      catch (Exception ex) {
        var res = runner.BuildErrorResult(op, ex);
        Log.Error(ex, "unhandled RunOperation exception, {@ErrorResults}", res);
        if (throws) throw;
        return res;
      }
    }

    async Task<ObjectState<O>> SaveOp(OperationStateAndConfig<C, O> op, DateTime start, R res) {
      var message = $"operation [{op.State.System}/{op.State.Stage}/{op.State.Object}] completed [{res.Result}] message: {res.Message}";
      var newstate = res.Result == EOperationResult.Success ? 
          op.State.Success(start, res.AbortVote, message) :
          op.State.Error(start, res.AbortVote, message, res.Exception?.ToString());
      return await ctl.GetObjectStateRepo<O>().SaveObjectState(newstate);
    }
  }

  public void Dispose() { Config.Dispose(); }

}