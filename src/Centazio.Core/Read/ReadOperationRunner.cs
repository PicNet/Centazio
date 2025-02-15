using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Read;

public class ReadOperationRunner(IEntityStager stager) : IOperationRunner<ReadOperationConfig> {

  public async Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.OpConfig.GetUpdatesAfterCheckpoint(op);
    if (res is ListReadOperationResult lr) await stager.Stage(op.State.System, op.OpConfig.SystemEntityTypeName, lr.PayloadList);
    return res;
  }
}