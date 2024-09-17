using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;

internal class WriteOperationRunner<C>(
      IEntityIntraSystemMappingStore entitymap,
      ICoreStorageGetter core) : 
    IOperationRunner<WriteOperationConfig, WriteOperationResult<C>> where C : ICoreEntity {
  
  public async Task<WriteOperationResult<C>> RunOperation(OperationStateAndConfig<WriteOperationConfig> op) {
    var pending = await core.Get<C>(op.Checkpoint);
    var maps = await entitymap.Get(pending, op.State.System);
    var results = op.Settings switch {
      SingleWriteOperationConfig<C> swo => await swo.WriteEntitiesToTargetSystem(swo, maps),
      BatchWriteOperationConfig<C> bwo => await bwo.WriteEntitiesToTargetSystem(bwo, maps),
      _ => throw new NotSupportedException()
    };
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `WriteEntitiesToTargetSystem`");
      return results;  
    }
    if (!results.EntitiesWritten.Any()) return results;
    
    var created = results.EntitiesWritten.Where(e => e.Map.Status == EEntityMappingStatus.Pending).ToList();
    var updated = results.EntitiesWritten.Where(e => e.Map.Status != EEntityMappingStatus.Pending).ToList();
    if (created.Any()) await entitymap.Create(created.Select(e => new CreateSuccessIntraSystemMapping(e.Core, op.State.System, e.Map.TargetId)));
    if (updated.Any()) await entitymap.Update(updated.Select(e => new UpdateSuccessEntityIntraSystemMapping(e.Map.Key, e.Map.Status)));
    return results;
  }

  public WriteOperationResult<C> BuildErrorResult(OperationStateAndConfig<WriteOperationConfig> op, Exception ex) => new ErrorWriteOperationResult<C>(EOperationAbortVote.Abort, ex);
}
