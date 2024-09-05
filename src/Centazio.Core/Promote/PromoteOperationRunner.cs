using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Func;

internal class PromoteOperationRunner(IStagedEntityStore staged) : IOperationRunner<PromoteOperationConfig> {
  
  public async Task<OperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<PromoteOperationConfig> op) {
    var pending = await staged.Get(op.Checkpoint, op.State.System, op.State.Object);
    return await op.Settings.Impl(op, pending); 
  }

}