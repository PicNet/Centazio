using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

internal class PromoteOperationRunner(IStagedEntityStore staged) 
    : IOperationRunner<PromoteOperationConfig, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<PromoteOperationConfig> op) {
    var pending = await staged.Get(op.Checkpoint, op.State.System, op.State.Object);
    var results = await op.Settings.PromoteObjects(op, pending);
    
    var topromote = results.Promoted.Select(e => e with { DatePromoted = funcstart });
    var toignore = results.Ignored.Select(e => e.Entity with { Ignore = e.Reason });
    
    await staged.Update(topromote.Concat(toignore));
    
    return results; 
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new([], [], EOperationResult.Error, ex.Message, EResultType.Error, 0, EOperationAbortVote.Abort, ex);

}