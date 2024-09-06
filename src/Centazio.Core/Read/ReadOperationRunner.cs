using System.Diagnostics;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Read;

internal class ReadOperationRunner(IEntityStager stager) : IOperationRunner<ReadOperationConfig> {

  public async Task<OperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.Settings.GetObjectsToStage(op);
    if (res.IsValid) await DoStage();
    return res;

    async Task DoStage() {
      if (res is OperationResult.SingleRecordOperationResult sr)
        await stager.Stage(funcstart, op.State.System, op.Settings.Object, sr.Payload);
      else if (res is OperationResult.ListRecordOperationResult lr)
        await stager.Stage(funcstart, op.State.System, op.Settings.Object, lr.PayloadList.Value);
      else throw new UnreachableException();
    }
  }
}