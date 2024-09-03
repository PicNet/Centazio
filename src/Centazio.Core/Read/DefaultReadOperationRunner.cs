using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Func;

internal class DefaultReadOperationRunner(IEntityStager stager, ICtlRepository ctl) : IReadOperationRunner {
  

  public async Task<ReadOperationResult> RunOperation(DateTime start, ReadOperationStateAndConfig op) {
    // run operation (get data) and validate results
    var res = (await op.Settings.Impl(start, op)).Validate();
    
    switch (res.Result) {
      case EOperationReadResult.Success: Log.Debug("read operation succeeded {@Operation} {@Results}", op, res); break;
      case EOperationReadResult.Warning: Log.Warning("read operation warning {@Operation} {@Results}", op, res); break;
      case EOperationReadResult.FailedRead: Log.Error("read operation failed {@Operation} {@Results}", op, res); break;
      default: throw new UnreachableException();
    }
    var stage = res.Result != EOperationReadResult.FailedRead && res.PayloadLength > 0; 
    
    if (stage) {
      if (res is SingleRecordReadOperationResult sr) {
        await stager.Stage(start, op.State.System, op.Settings.Object, sr.Payload);
      } else if (res is ListRecordReadOperationResult lr) {
        await stager.Stage(start, op.State.System, op.Settings.Object, lr.PayloadList);
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