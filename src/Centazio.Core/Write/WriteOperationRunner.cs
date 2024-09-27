using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;
public class WriteOperationRunner<C>(IEntityIntraSystemMappingStore entitymap, ICoreStorageGetter core) : 
    IOperationRunner<C, WriteOperationResult> where C : WriteOperationConfig {
  
  public async Task<WriteOperationResult> RunOperation(OperationStateAndConfig<C> op) {
    var pending = await core.Get(op.State.CoreEntityType, op.Checkpoint);
    var maps = await entitymap.GetForCores(pending, op.State.System, op.State.CoreEntityType);
    var results = await op.Settings.WriteEntitiesesToTargetSystem.WriteEntities(op.Settings, maps.Created, maps.Updated);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `WriteEntitiesToTargetSystem`");
      return results;  
    }
    if (results.EntitiesCreated.Any()) await entitymap.Create(results.EntitiesCreated.Select(e => e.Map));
    if (results.EntitiesUpdated.Any()) await entitymap.Update(results.EntitiesUpdated.Select(e => e.Map));
    return results;
  }

  public WriteOperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult(EOperationAbortVote.Abort, ex);
}
