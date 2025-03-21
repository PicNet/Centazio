using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

public abstract class PromoteFunction(SystemName system, IStagedEntityRepository stage, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<PromoteOperationConfig>(system, LifecycleStage.Defaults.Promote, ctl) {

  public override async Task<OperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig> op) {
    var steps = new PromotionSteps(core, ctl, op);
    await steps.LoadPendingStagedEntities(stage);
    await steps.LoadExistingCoreEntities();
    await steps.ApplyChangesToCoreEntities();
    steps.IgnoreUpdatesToSameEntityInBatch();
    
    if (op.OpConfig.IsBidirectional) await steps.IdentifyBouncedBackAndSetCorrectId(); 
    else steps.IgnoreEntitiesBouncingBack();
    
    steps.IgnoreNonMeaninfulChanges();
    await steps.WriteEntitiesToCoreStorageAndUpdateMaps();
    await steps.UpdateAllStagedEntitiesWithNewState(stage);
    steps.LogPromotionSteps();
    
    return steps.GetResults();

  }
}