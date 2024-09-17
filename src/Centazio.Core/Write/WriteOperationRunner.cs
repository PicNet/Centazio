using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;

public interface IWriteEntitysToTargetSystemImpl<C> where C : ICoreEntity {
  public Task<WriteOperationResult<C>> Run(List<C> pending);
} 


public interface IWriteSingleEntityToTargetSystemCallback<C> : IWriteEntitysToTargetSystemImpl<C> where C : ICoreEntity {
  Task EntityWritten((C Core, EntityIntraSystemMapping Map) map);
}

public interface IWriteBatchEntiiestToTargetSystemCallback<C> : IWriteEntitysToTargetSystemImpl<C> where C : ICoreEntity {
  Task EntitiesWritten(List<(C Core, EntityIntraSystemMapping Map)> maps);
}



public class WriteSingleEntityToTargetSystemCallback<C>(SingleWriteOperationConfig<C> op) 
    : IWriteSingleEntityToTargetSystemCallback<C> where C : ICoreEntity {
  
  public Task<WriteOperationResult<C>> Run(List<C> pending) {
    return op.WriteEntitiesToTargetSystem(op, pending, this);
  }
  
  public Task EntityWritten((C Core, EntityIntraSystemMapping Map) map) {
    return Task.CompletedTask;
  }
  

}

public class WriteBatchEntiiestToTargetSystemCallback<C>(BatchWriteOperationConfig<C> op) 
    : IWriteBatchEntiiestToTargetSystemCallback<C> where C : ICoreEntity {

  public Task<WriteOperationResult<C>> Run(List<C> pending) {
    return op.WriteEntitiesToTargetSystem(op, pending, this);
  }
  
  public Task EntitiesWritten(List<(C Core, EntityIntraSystemMapping Map)> cores) {
    return Task.CompletedTask;
  }
}

internal class WriteOperationRunner<C>(
      IEntityIntraSystemMappingStore entitymap,
      ICoreStorageGetter core) : 
    IOperationRunner<WriteOperationConfig<C>, WriteOperationResult<C>> where C : ICoreEntity {
  
  public async Task<WriteOperationResult<C>> RunOperation(OperationStateAndConfig<WriteOperationConfig<C>> op) {
    var pending = await core.Get<C>(op.Checkpoint);
    var results = op.Settings switch {
      SingleWriteOperationConfig<C> swo => await new WriteSingleEntityToTargetSystemCallback<C>(swo).Run(pending),
      BatchWriteOperationConfig<C> bwo => await new WriteBatchEntiiestToTargetSystemCallback<C>(bwo).Run(pending),
      _ => throw new NotSupportedException()
    };
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `WriteEntitiesToTargetSystem`");
      return results;  
    }
    if (!results.EntitiesWritten.Any()) return results;
    
    var maps = await entitymap.Get(results.EntitiesWritten, op.State.System);
    var created = maps.Where(m => m.Map.Status == EEntityMappingStatus.Pending).ToList();
    var updated = maps.Where(m => m.Map.Status != EEntityMappingStatus.Pending).ToList();
    if (created.Any()) await entitymap.Create(created.Select(m => new CreateSuccessIntraSystemMapping(m.Core, op.State.System, m.Map.TargetId)));
    if (updated.Any()) await entitymap.Update(updated.Select(m => new UpdateSuccessEntityIntraSystemMapping(m.Map.Key)));
    return results;
  }

  public WriteOperationResult<C> BuildErrorResult(OperationStateAndConfig<WriteOperationConfig<C>> op, Exception ex) => new ErrorWriteOperationResult<C>(EOperationAbortVote.Abort, ex);
}
