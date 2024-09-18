using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;

public class WriteOperationRunner<E, C>(IEntityIntraSystemMappingStore entitymap, ICoreStorageGetter core) : 
    IOperationRunner<C, WriteOperationResult<E>> 
        where E : ICoreEntity 
        where C : WriteOperationConfig {
  
  public async Task<WriteOperationResult<E>> RunOperation(OperationStateAndConfig<C> op) {
    var pending = await core.Get<E>(op.Checkpoint);
    var maps = await entitymap.Get(pending, op.State.System);
    var results = op.Settings switch {
      SingleWriteOperationConfig<E> swo => await swo.WriteEntitiesToTargetSystem.WriteEntities(swo, maps),
      BatchWriteOperationConfig<E> bwo => await bwo.WriteEntitiesToTargetSystem.WriteEntities(bwo, maps),
      _ => throw new NotSupportedException()
    };
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `WriteEntitiesToTargetSystem`");
      return results;  
    }
    if (!results.EntitiesWritten.Any()) return results;
    // todo: protect code from this scenario
    if (results.EntitiesWritten.Any(e => e.Map.TargetId == EEntityMappingStatus.Pending.ToString())) throw new Exception($"created entities do not have TargetId set");
    
    var created = results.EntitiesWritten.Where(e => e.Map.Status == EEntityMappingStatus.SuccessCreate).ToList();
    var updated = results.EntitiesWritten.Where(e => e.Map.Status == EEntityMappingStatus.SuccessUpdate).ToList();
    
    if (created.Any()) await entitymap.Create(created.Select(e => new CreateSuccessIntraSystemMapping(e.Core, op.State.System, e.Map.TargetId)));
    if (updated.Any()) await entitymap.Update(updated.Select(e => new UpdateSuccessEntityIntraSystemMapping(e.Map.Key)));
    return results;
  }

  public WriteOperationResult<E> BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult<E>(EOperationAbortVote.Abort, ex);
}
