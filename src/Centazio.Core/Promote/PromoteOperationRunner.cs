using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

public class PromoteOperationRunner(
    IStagedEntityRepository stagestore,
    ICoreStorage core,
    ICtlRepository ctl) : IOperationRunner<PromoteOperationConfig> {
  
  public async Task<OperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig> op) {
    var steps = new PromotionSteps(core, ctl, op);
    await steps.LoadPendingStagedEntities(stagestore);
    await steps.LoadExistingCoreEntities();
    await steps.ApplyChangesToCoreEntities();
    steps.IgnoreUpdatesToSameEntityInBatch();
    
    if (op.OpConfig.IsBidirectional) await steps.IdentifyBouncedBackAndSetCorrectId(); 
    else steps.IgnoreEntitiesBouncingBack();
    
    steps.IgnoreNonMeaninfulChanges();
    await steps.WriteEntitiesToCoreStorageAndUpdateMaps();
    await steps.UpdateAllStagedEntitiesWithNewState(stagestore);
    steps.LogPromotionSteps();
    
    return steps.GetResults();

  }
}
