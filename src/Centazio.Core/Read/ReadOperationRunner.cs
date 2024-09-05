using System.Diagnostics;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Func;

internal class ReadOperationRunner(IEntityStager stager) : IOperationRunner<ReadOperationConfig> {

  public async Task<OperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.Settings.Impl(op);
    
    switch (res.Result) {
      case EOperationResult.Success: Log.Debug("read operation succeeded {@Operation} {@Results}", op, res); break;
      case EOperationResult.Warning: Log.Warning("read operation warning {@Operation} {@Results}", op, res); break;
      case EOperationResult.Error: Log.Error("read operation failed {@Operation} {@Results}", op, res); break;
      default: throw new UnreachableException();
    }
    var stage = res.Result != EOperationResult.Error && res.ResultLength > 0;
    if (stage) await DoStage(funcstart, op, res);
    return res;
  }

  private async Task DoStage(DateTime start, OperationStateAndConfig<ReadOperationConfig> op, OperationResult res) {
    switch (res) {
      case SingleRecordOperationResult sr: await stager.Stage(start, op.State.System, op.Settings.Object, sr.Payload);
        break;
      case ListRecordOperationResult lr: await stager.Stage(start, op.State.System, op.Settings.Object, lr.PayloadList.Value);
        break;
      default: throw new UnreachableException();
    }
  }

}