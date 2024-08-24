using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Helpers;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Func;

public class ReadFunctionComposer(
    ReadFunctionConfig cfg,
    EntityStager stager,
    IUtcDate utc,
    ICtlRepository ctl,
    IReadOperationRunner runner,
    IReadOperationsFilterAndPrioritiser? prioritiser = null) {
  
  private IReadOperationsFilterAndPrioritiser Prioritiser { get; } = prioritiser ?? new DefaultReadOperationsFilterAndPrioritiser();
  
  public async Task<string> Run() {
    var now = utc.Now;
    ValidateConfig();
    
    var sysstate = await ctl.GetOrCreateSystemState(cfg.System, cfg.Stage);
    var states = await LoadOperationsStates(sysstate);
    var ready = GetReadyOperations(now, states);
    var prioritised = Prioritiser.Prioritise(ready);
    var results = await prioritised
        .Select(op => RunOperation(now, op))
        .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);
    return CombineSummaryResults(results);
    
    void ValidateConfig() { if (!cfg.Operations.Any()) throw new Exception($"System {cfg.System} Read configuration has no operations defined"); }
  }

  private async Task<ReadOperationStateAndConfig[]> LoadOperationsStates(SystemState system) => 
      await Task.WhenAll(
          cfg.Operations.Select(async op => 
              new ReadOperationStateAndConfig(await ctl.GetOrCreateObjectState(system, op.Object), op)));

  private IEnumerable<ReadOperationStateAndConfig> GetReadyOperations(DateTime now, IEnumerable<ReadOperationStateAndConfig> states) {
    bool IsOperationReady(ReadOperationStateAndConfig op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.UtcNow.AddYears(-10));
      return next <= now;
    }
    return states.Where(IsOperationReady);
  }

  private async Task<ReadOperationResults> RunOperation(DateTime start, ReadOperationStateAndConfig op) {
    var res = await runner.Run(start, op);
    res.Validate();
    
    if (res.Result == EOperationReadResult.FailedRead) { 
      Log.Error("Read operation {@Operation} failed {@Results}", op, res);
      return res; // do not stage
    }
    if (res.Result == EOperationReadResult.Warning) {
      Log.Warning("Read operation {@Operation} warning {@Results}", op, res);
    } else {
      Log.Debug("Read operation {@Operation} succeeded {@Results}", op, res);
    }
    if (res.PayloadLength > 0) {
      await (res switch {
        SingleRecordReadOperationResults sr => stager.Stage(start, cfg.System, op.Settings.Object, sr.Payload),
        ListRecordReadOperationResults sr => stager.Stage(start, cfg.System, op.Settings.Object, sr.PayloadList),
        _ => throw new Exception()
      });
    }
    
    var newstate = op.State with {
      LastStart = start,
      LastCompleted = utc.Now,
      LastResult = res.Result,
      LastAbortVote = res.AbortVote,
      LastRunMessage = res.Message,
      LastPayLoadType = res.PayloadType,
      LastPayLoadLength = res.PayloadLength,
      LastRunException = res.Exception?.ToString()
    };
    await ctl.SaveObjectState(newstate);
    Log.Debug("Read operation {@Operation} with results {@Results}.  New state saved {@newstate}", op, res, newstate);
    return res;
  }

  private string CombineSummaryResults(IEnumerable<ReadOperationResults> results) {
    var message = String.Join(';', results.Select(r => r.ToString()));
    Log.Information("Read Function for system {@System} completed: {message}", cfg.System, message);
    return message;
  }
}