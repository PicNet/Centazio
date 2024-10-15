using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

public class PromoteOperationRunner(
    IStagedEntityStore stagestore,
    ICoreStorage core,
    ICoreToSystemMapStore entitymap) : IOperationRunner<PromoteOperationConfig, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig> op) {
    var steps = new PromotionSteps(core, entitymap, op);
    await steps.LoadPendingStagedEntities(stagestore);
    steps.DeserialisePendingStagedEntities();
    await steps.LoadExistingCoreEntities();
    await steps.ApplyChangesToCoreEntities();
    steps.IgnoreUpdatesToSampleEntityInBatch();
    await steps.HandleEntitiesBouncingBack();
    await steps.IgnoreNonMeaninfulChanges();
    await steps.WriteEntitiesToCoreStorageAndUpdateMaps();
    await steps.UpdateAllStagedEntitiesWithNewState(stagestore);
    
    // todo: add thoroughly helpful debuggning
    // Log.Information($"PromoteOperationRunner[{op.State.System}/{op.State.Object}] Bidi[{op.OpConfig.IsBidirectional}] Pending[{pending.Count}] ToPromote[{topromote.Count}] Meaningful[{meaningful.Count}] ToIgnore[{toignore.Count}]");
    
    
    return new SuccessPromoteOperationResult([], []);

  }
  

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(EOperationAbortVote.Abort, ex);
  
  
}
