using System.Diagnostics;
using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Func;

internal class DefaultReadOperationRunner(
    Func<DateTime, ReadOperationStateAndConfig, Task<ReadOperationResult>> impl, 
    IEntityStager stager, 
    IUtcDate now, 
    ICtlRepository ctl) : IReadOperationRunner {
  

  public async Task<ReadOperationResult> RunOperation(DateTime start, ReadOperationStateAndConfig op) {
    var res = await impl(start, op);
    res.Validate();
    
    switch (res.Result) {
      case EOperationReadResult.Success: Log.Debug("Read operation {@Operation} succeeded {@Results}", op, res); break;
      case EOperationReadResult.Warning: Log.Warning("Read operation {@Operation} warning {@Results}", op, res); break;
      case EOperationReadResult.FailedRead: Log.Error("Read operation {@Operation} failed {@Results}", op, res); break;
      default: throw new Exception($"Read operation {op} resulted in an invalid result {res.Result}");
    }
    var stage = res.Result != EOperationReadResult.FailedRead && 
        res.PayloadLength > 0; 
    
    if (stage) {
      if (res is SingleRecordReadOperationResult sr) {
        await stager.Stage(start, op.State.System, op.Settings.Object, sr.Payload);
      } else if (res is ListRecordReadOperationResult lr) {
        await stager.Stage(start, op.State.System, op.Settings.Object, lr.PayloadList);
      } else throw new UnreachableException();
    }
    
    var newstate = op.State with {
      LastStart = start,
      LastCompleted = now.Now,
      LastResult = res.Result,
      LastAbortVote = res.AbortVote,
      LastRunMessage = $"Read operation [{op.State.System}/{op.State.Stage}/{op.State.Object}] completed [{res.Result}] - staged[{stage}] message: " + res.Message,
      LastPayLoadType = res.PayloadType,
      LastPayLoadLength = res.PayloadLength,
      LastRunException = res.Exception?.ToString()
    };
    await ctl.SaveObjectState(newstate);
    Log.Debug("Read operation {@Operation} with results {@Results}.  New state saved {@newstate}", op, res, newstate);
    return res;
  }
}