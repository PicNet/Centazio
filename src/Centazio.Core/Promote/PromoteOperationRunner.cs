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
    steps.IgnoreUpdatesToSameEntityInBatch();
    
    if (op.OpConfig.IsBidirectional) await steps.IdentifyBouncedBackAndSetCorrectId(); 
    else steps.IgnoreEntitiesBouncingBack();
    
    await steps.IgnoreNonMeaninfulChanges();
    await steps.WriteEntitiesToCoreStorageAndUpdateMaps();
    await steps.UpdateAllStagedEntitiesWithNewState(stagestore);
    steps.LogPromotionSteps();
    
    return steps.GetResults();

  }
  

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(EOperationAbortVote.Abort, ex);
  
  
}
