using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;
public class WriteOperationRunner<E, C>(IEntityIntraSystemMappingStore entitymap, ICoreStorageGetter core) : 
    IOperationRunner<C, WriteOperationResult> 
        where E : ICoreEntity 
        where C : WriteOperationConfig {
  
  public async Task<WriteOperationResult> RunOperation(OperationStateAndConfig<C> op) {
    var pending = await core.Get<E>(op.Checkpoint);
    var maps = await entitymap.GetForCores(pending, op.State.System);
    var results = op.Settings switch {
      SingleWriteOperationConfig swo => await swo.WriteEntitiesToTargetSystem.WriteEntities(swo, maps.Created, maps.Updated),
      BatchWriteOperationConfig bwo => await bwo.WriteEntitiesToTargetSystem.WriteEntities(bwo, maps.Created, maps.Updated),
      _ => throw new NotSupportedException()
    };
    
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
