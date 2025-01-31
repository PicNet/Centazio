using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Serilog;

namespace Centazio.Core.Runner;

public interface IRunnableFunction : IDisposable {
  SystemName System { get; }
  LifecycleStage Stage { get; } 
  Task<FunctionRunResults> RunFunction();
}

public abstract class AbstractFunction<C> : IRunnableFunction where C : OperationConfig {

  public SystemName System { get; }
  public LifecycleStage Stage { get; }
  
  protected DateTime FunctionStartTime { get; private set; }
  protected FunctionConfig<C> Config { get; }
  
  private bool running;
  private readonly IOperationRunner<C> oprunner;
  private readonly ICtlRepository ctl;

  protected AbstractFunction(SystemName system, LifecycleStage stage, IOperationRunner<C> oprunner, ICtlRepository ctl) {
    System = system;
    Stage = stage;
    
    Config = GetFunctionConfiguration();
    
    this.oprunner = oprunner;
    this.ctl = ctl;
  }

  protected abstract FunctionConfig<C> GetFunctionConfiguration();

  public async Task<FunctionRunResults> RunFunction() {
    // prevent race-conditions with slow databases
    if (running) return new AlreadyRunningFunctionRunResults();

    try {
      running = true;
      return await RunFunctionImpl();
    } finally { running = false; }

    async Task<FunctionRunResults> RunFunctionImpl() {
      FunctionStartTime = UtcDate.UtcNow;
  
      // Log.Debug("checking function [{@System}/{@Stage}] - {@Now}", System, Stage, UtcDate.UtcNow);
  
      var state = await ctl.GetOrCreateSystemState(System, Stage);
      if (!state.Active) {
        Log.Debug("function is inactive, ignoring run {@SystemState}", state);
        return new InactiveFunctionRunResults();
      }
      if (state.Status != ESystemStateStatus.Idle) {
        var minutes = UtcDate.UtcNow.Subtract(state.LastStarted ?? throw new UnreachableException()).TotalMinutes;
        if (minutes <= Config.TimeoutMinutes) {
          Log.Debug("function is already running, ignoring run {@SystemState}", state);
          return new AlreadyRunningFunctionRunResults();
        }
        Log.Information("function considered stuck, running again {@SystemState} {@MaximumRunningMinutes} {@MinutesSinceStart}", state, Config.TimeoutMinutes, minutes);
      }
  
      try {
        // not setting last start here as we need the LastStart to represent the time the function was started before this run
        state = await ctl.SaveSystemState(state.Running());
        var results = await RunFunctionOperations(state);
        await SaveCompletedState();
        return new SuccessFunctionRunResults(results);
      } catch (Exception ex) {
        await SaveCompletedState();
        Log.Error(ex, "unhandled function error, returning empty OpResults {@SystemState}", state);
        if (Config.ThrowExceptions) throw;
        return new ErrorFunctionRunResults(ex);
      }

      async Task SaveCompletedState() => await ctl.SaveSystemState(state.Completed(FunctionStartTime));
    }
  }

  protected virtual async Task<List<OperationResult>> RunFunctionOperations(SystemState sys) {
    var opstates = await LoadOperationsStates(Config, sys, ctl);
    var readyops = GetReadyOperations(opstates);
    return await RunOperationsTillAbort(readyops, oprunner, ctl, Config.ThrowExceptions);
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
  
  internal static async Task<List<OperationResult>> RunOperationsTillAbort(List<OperationStateAndConfig<C>> ops, IOperationRunner<C> runner, ICtlRepository ctl, bool throws = true) {
    return await ops
        .Select(async op => await RunAndSaveOp(op))
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);

    async Task<OperationResult> RunAndSaveOp(OperationStateAndConfig<C> op) {
      var opstart = UtcDate.UtcNow;
      Log.Information("operation started - [{@System}/{@Stage}/{@Object}] checkpoint[{@Checkpoint}]", op.State.System, op.State.Stage, op.State.Object, op.Checkpoint);
      
      var result = await RunOp(op);
      
      await SaveOp(op, opstart, result);
      
      Log.Information("operation completed - [{@System}/{@Stage}/{@Object}] took[{@Took:0}ms] - {@Result}", op.State.System, op.State.Stage, op.State.Object, (UtcDate.UtcNow - opstart).TotalMilliseconds, result);
      
      return result;
    }
    
    async Task<OperationResult> RunOp(OperationStateAndConfig<C> op) {
      try { return await runner.RunOperation(op); } 
      catch (Exception ex) {
        var res = runner.BuildErrorResult(op, ex);
        if (res.Result == EOperationResult.Success) throw new Exception("BuildErrorResult should never return a 'Success' result");
        Log.Error(ex, "unhandled RunOperation exception, {@ErrorResults}", res);
        if (throws) throw;
        return res;
      }
    }

    async Task SaveOp(OperationStateAndConfig<C> op, DateTime start, OperationResult res) {
      var message = $"operation [{op.State.System}/{op.State.Stage}/{op.State.Object}] completed [{res.Result}] message: {res.Message}";
      var newstate = res.Result == EOperationResult.Success ? 
          op.State.Success(res.NextCheckpoint ?? start, start, res.AbortVote, message) :
          op.State.Error(start, res.AbortVote, message, res.Exception?.ToString());
      await ctl.SaveObjectState(newstate);
    }
  }

  public void Dispose() { Config.Dispose(); }

}

public abstract record FunctionRunResults(List<OperationResult> OpResults, string Message); 
public record SuccessFunctionRunResults(List<OperationResult> OpResults) : FunctionRunResults(OpResults, "SuccessFunctionRunResults");
public record AlreadyRunningFunctionRunResults() : FunctionRunResults([], "AlreadyRunningFunctionRunResults");
public record InactiveFunctionRunResults() : FunctionRunResults([], "InactiveFunctionRunResults");
// ReSharper disable once NotAccessedPositionalProperty.Global
public record ErrorFunctionRunResults(Exception Exception) : FunctionRunResults([], Exception.ToString());