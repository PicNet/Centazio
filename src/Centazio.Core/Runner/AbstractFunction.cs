﻿using Centazio.Core.Ctl;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Settings;
using Centazio.Core.Write;

namespace Centazio.Core.Runner;

public interface IRunnableFunction : IDisposable {
  
  SystemName System { get; }
  LifecycleStage Stage { get; }
  bool Running { get; }
  FunctionConfig Config { get; }
  
  Task RunFunctionOperations(SystemState sys, List<FunctionTrigger> triggers, List<OpResultAndObject> runningresults);
  
  bool IsTriggeredBy(ObjectChangeTrigger trigger);
  
  ValidString GetFunctionPollCronExpression(DefaultsSettings defs) {
    return new ValidString(Config.FunctionPollExpression 
        ?? (this is ReadFunction ? defs.ReadFunctionPollExpression : 
            this is PromoteFunction ? defs.PromoteFunctionPollExpression : 
            this is WriteFunction ? defs.WriteFunctionPollExpression : 
            defs.OtherFunctionPollExpression));
  } 
}

public abstract class AbstractFunction<C> : IRunnableFunction where C : OperationConfig {

  public SystemName System { get; }
  public LifecycleStage Stage { get; }
  
  public FunctionConfig Config { get; }
  
  public bool Running { get; private set; }
  
  protected readonly ICtlRepository ctl;
  protected DateTime FunctionStartTime { get; private set; }
  
  private readonly List<ObjectChangeTrigger> triggers;
  
  protected AbstractFunction(SystemName system, LifecycleStage stage, ICtlRepository ctl) {
    (System, Stage, this.ctl) = (system, stage, ctl);
    Config = GetFunctionConfiguration();
    triggers = Config.Operations.SelectMany(op => op.Triggers).Distinct().ToList();
  }

  protected abstract FunctionConfig GetFunctionConfiguration();

  public virtual async Task RunFunctionOperations(SystemState sys, List<FunctionTrigger> triggeredby, List<OpResultAndObject> runningresults) {
    if (Running) throw new Exception("function is already running");
    (FunctionStartTime, Running) = (UtcDate.UtcNow, true);
    try {
      var opstates = await LoadOperationsStates(Config, sys, ctl);
      var readyops = GetReadyOperations(opstates, triggeredby);
      await RunOperationsTillAbort(readyops, runningresults, Config.ThrowExceptions);
    } finally { Running = false; }
  }
  
  public bool IsTriggeredBy(ObjectChangeTrigger trigger) => 
      triggers.Any(functrigger => functrigger.Matches(trigger));

  internal static async Task<List<OperationStateAndConfig<C>>> LoadOperationsStates(FunctionConfig conf, SystemState system, ICtlRepository ctl) {
    return (await conf.Operations
            .Cast<C>()
            .Select(async op => {
      var state = await ctl.GetOrCreateObjectState(system, op.Object, op.FirstTimeCheckpoint ?? conf.DefaultFirstTimeCheckpoint);
      return new OperationStateAndConfig<C>(state, conf, op, state.NextCheckpoint);
    }).Synchronous())
    .Where(op => op.State.Active)
    .ToList();
  }

  internal static List<OperationStateAndConfig<C>> GetReadyOperations(List<OperationStateAndConfig<C>> states, List<FunctionTrigger> triggeredby) {
    bool IsOperationReady(OperationStateAndConfig<C> op) {
      var objtriggers = triggeredby.OfType<ObjectChangeTrigger>().ToList();
      if (objtriggers.Any() && !op.OpConfig.ShouldRunBasedOnTriggers(objtriggers)) return false;
      var next = op.OpConfig.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.MinValue.ToUniversalTime());
      return next <= UtcDate.UtcNow;
    }
    return states.Where(IsOperationReady).ToList();
  }
  
  internal async Task RunOperationsTillAbort(List<OperationStateAndConfig<C>> ops, List<OpResultAndObject> runningresults, bool throws = true) {
    await ops
        .Select(async op => runningresults.AddAndReturn(await RunAndSaveOp(op)))
        .Synchronous(r => r.Result.AbortVote == EOperationAbortVote.Abort);

    async Task<OpResultAndObject> RunAndSaveOp(OperationStateAndConfig<C> op) {
      var opstart = UtcDate.UtcNow;
      Log.Information("operation started - [{@System}/{@Stage}/{@Object}] checkpoint[{@Checkpoint}]", op.State.System, op.State.Stage, op.State.Object, op.Checkpoint);
      
      var result = await RunOp(op);
      await SaveOp(op, opstart, result);
      
      Log.Information("operation completed - [{@System}/{@Stage}/{@Object}] took[{@Took:0}ms] - {@Result}", op.State.System, op.State.Stage, op.State.Object, (UtcDate.UtcNow - opstart).TotalMilliseconds, result.Result.Message);
      
      return result;
    }
    
    async Task<OpResultAndObject> RunOp(OperationStateAndConfig<C> op) {
      try {
        var result = await RunOperation(op);
        return new OpResultAndObject(op.State.Object, result);
      } catch (Exception ex) {
        Log.Error(ex, "unhandled RunOperation exception {@ErrorMessage}", ex.Message);
        if (throws) throw;
        // note: the '0' argument to `ErrorOperationResult` means that no changes will be notified, it
        //    is possible for functions to have partial success (when they process items sequentially and
        //    not in a batch) but this is a rare occurrence and will be addressed if encountered in
        //    the wild.  However, any solution will be ugly as we will need to somehow track a counter
        //    of successful changes and that will be hard to implicitly document in the api.
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

  public abstract Task<OperationResult> RunOperation(OperationStateAndConfig<C> op);
  
  public void Dispose() { Config.Dispose(); }
}