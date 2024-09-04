using centazio.core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Func;

internal class PromoteOperationRunner(IStagedEntityStore staged) : IOperationRunner<PromoteOperationConfig> {
  
  public async Task<OperationResult> RunOperation(DateTime start, OperationStateAndConfig<PromoteOperationConfig> op) {
    var pending = await staged.Get(op.State.LastStart ?? DateTime.MinValue, op.State.System, op.State.Object);
    return await op.Settings.Impl(start, op, pending); 
  }

}