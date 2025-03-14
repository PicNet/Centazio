using Centazio.Core.Ctl;

namespace Centazio.Core.Runner;

public interface IRunnableFunction : IDisposable {
  SystemName System { get; }
  LifecycleStage Stage { get; } 
  Task<List<OpResultAndObject>> RunFunctionOperations(SystemState sys);
}

public abstract class AbstractFunction<C> : IRunnableFunction where C : OperationConfig {

  public SystemName System { get; }
  public LifecycleStage Stage { get; }
  public FunctionConfig<C> Config { get; }
  
  public bool Running { get; private set; }
  
  protected readonly ICtlRepository ctl;
  public DateTime FunctionStartTime { get; private set; }
  
  protected AbstractFunction(SystemName system, LifecycleStage stage, ICtlRepository ctl) {
    System = system;
    Stage = stage;
    
    this.ctl = ctl;
    
    Config = GetFunctionConfiguration();
  }

  public abstract FunctionConfig<C> GetFunctionConfiguration();
  public abstract Task<OperationResult> RunOperation(OperationStateAndConfig<C> op);

  public virtual async Task<List<OpResultAndObject>> RunFunctionOperations(SystemState sys) {
    if (Running) throw new Exception("function is already running");
    (FunctionStartTime, Running) = (UtcDate.UtcNow, true);
    try {
      var opstates = await LoadOperationsStates(Config, sys, ctl);
      var readyops = GetReadyOperations(opstates);
      return await RunOperationsTillAbort(readyops, Config.ThrowExceptions);
    } finally { Running = false; }
  }

  internal static async Task<List<OperationStateAndConfig<C>>> LoadOperationsStates(FunctionConfig<C> conf, SystemState system, ICtlRepository ctl) {
    return (await conf.Operations
            .Select(async op => {
      var state = await ctl.GetOrCreateObjectState(system, op.Object, op.FirstTimeCheckpoint ?? conf.DefaultFirstTimeCheckpoint);
      return new OperationStateAndConfig<C>(state, conf, op, state.NextCheckpoint);
    }).Synchronous())
    .Where(op => op.State.Active)
    .ToList();
  }

  internal static List<OperationStateAndConfig<C>> GetReadyOperations(List<OperationStateAndConfig<C>> states) {
    bool IsOperationReady(OperationStateAndConfig<C> op) {
      var next = op.OpConfig.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.MinValue.ToUniversalTime());
      return next <= UtcDate.UtcNow;
    }
    return states.Where(IsOperationReady).ToList();
  }
  
  internal async Task<List<OpResultAndObject>> RunOperationsTillAbort(List<OperationStateAndConfig<C>> ops, bool throws = true) {
    return await ops
        .Select(async op => await RunAndSaveOp(op))
        .Synchronous(r => r.Result.AbortVote == EOperationAbortVote.Abort);

    async Task<OpResultAndObject> RunAndSaveOp(OperationStateAndConfig<C> op) {
      var opstart = UtcDate.UtcNow;
      Log.Information("operation started - [{@System}/{@Stage}/{@Object}] checkpoint[{@Checkpoint}]", op.State.System, op.State.Stage, op.State.Object, op.Checkpoint);
      
      var result = await RunOp(op);
      
      await SaveOp(op, opstart, result);
      
      Log.Information("operation completed - [{@System}/{@Stage}/{@Object}] took[{@Took:0}ms] - {@Result}", op.State.System, op.State.Stage, op.State.Object, (UtcDate.UtcNow - opstart).TotalMilliseconds, result);
      
      return result;
    }
    
    async Task<OpResultAndObject> RunOp(OperationStateAndConfig<C> op) {
      try {
        var result = await RunOperation(op);
        return new OpResultAndObject(op.State.Object, result);
      } catch (Exception ex) {
        Log.Error(ex, "unhandled RunOperation exception {@ErrorMessage}", ex.Message);
        if (throws) throw;
        // todo: '0' here means that no changes will be notified, it is possible for functions to have partial success and should be supported
        return new OpResultAndObject(op.State.Object, new ErrorOperationResult(0, EOperationAbortVote.Abort, ex));
      }
    }

    async Task SaveOp(OperationStateAndConfig<C> op, DateTime start, OpResultAndObject result) {
      var res = result.Result;
      var message = $"operation [{op.State.System}/{op.State.Stage}/{op.State.Object}] completed [{res.Result}] message: {res.Message}";
      var newstate = res.Result == EOperationResult.Success ? 
          op.State.Success(res.NextCheckpoint ?? start, start, res.AbortVote, message) :
          op.State.Error(start, res.AbortVote, message, res.Exception?.ToString());
      await ctl.SaveObjectState(newstate);
    }
  }

  public void Dispose() { Config.Dispose(); }
}