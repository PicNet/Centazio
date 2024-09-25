using System.Diagnostics;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Read;

public class ReadOperationRunner(IEntityStager stager) : IOperationRunner<ReadOperationConfig, ReadOperationResult> {

  public async Task<ReadOperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.Settings.GetObjectsToStage.GetObjects(op);
    if (res.ResultLength > 0) await DoStage();
    return res;

    async Task DoStage() {
      if (res is SingleRecordReadOperationResult sr) {
        await stager.Stage(op.State.System, op.Settings.Object, sr.Payload);
      } else if (res is ListRecordsReadOperationResult lr) {
        await stager.Stage(op.State.System, op.Settings.Object, lr.PayloadList);
      } else throw new UnreachableException();
    }
  }

  public ReadOperationResult BuildErrorResult(OperationStateAndConfig<ReadOperationConfig> op, Exception ex) => 
      new ErrorReadOperationResult(EOperationAbortVote.Abort, ex);

}