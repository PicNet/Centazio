using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using centazio.core.Runner;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Func;

internal class ReadOperationRunner(IEntityStager stager, ICtlRepository ctl) : IOperationRunner {
  

  public async Task<OperationResult> RunOperation(DateTime start, OperationStateAndConfig op) {
    var res = await op.Settings.Impl(start, op);
    
    switch (res.Result) {
      case EOperationResult.Success: Log.Debug("read operation succeeded {@Operation} {@Results}", op, res); break;
      case EOperationResult.Warning: Log.Warning("read operation warning {@Operation} {@Results}", op, res); break;
      case EOperationResult.Error: Log.Error("read operation failed {@Operation} {@Results}", op, res); break;
      default: throw new UnreachableException();
    }
    var stage = res.Result != EOperationResult.Error && res.PayloadLength > 0; 
    
    if (stage) {
      if (res is SingleRecordOperationResult sr) {
        await stager.Stage(start, op.State.System, op.Settings.Object, sr.Payload);
      } else if (res is ListRecordOperationResult lr) {
        await stager.Stage(start, op.State.System, op.Settings.Object, lr.PayloadList.Value);
      } else throw new UnreachableException();
    }
    
    var newstate = op.State with {
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = res.Result,
      LastAbortVote = res.AbortVote,
      LastRunMessage = $"read operation [{op.State.System}/{op.State.Stage}/{op.State.Object}] completed [{res.Result}] - staged[{stage}] message: " + res.Message,
      LastPayLoadType = res.PayloadType,
      LastPayLoadLength = res.PayloadLength,
      LastRunException = res.Exception?.ToString()
    };
    await ctl.SaveObjectState(newstate);
    Log.Information("read operation completed {@Operation} {@Results} {@UpdatedSystemState}", op, res, newstate);
    return res;
  }
}