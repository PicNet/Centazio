using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Promote;

public class PromoteOperationRunner(
    IStagedEntityStore staged, 
    IEntityIntraSystemMappingStore entitymap,
    ICoreStorageUpserter core) : IOperationRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op) {
    var start = UtcDate.UtcNow;
    var pending = await staged.GetUnpromoted(op.Checkpoint, op.State.System, op.Config.ExternalEntityType);
    if (!pending.Any()) return new SuccessPromoteOperationResult([], []);
    
    var results = await op.Config.EvaluateEntitiesToPromote.Evaluate(op, pending);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `EvaluateEntitiesToPromote`.  Not promoting any entities, not updating StagedEntity states.");
      return results;  
    }
    
    if (results.ToPromote.Any()) await WriteEntitiesToCoreStorage(op, results.ToPromote.Select(p => p.Core).ToList());
    
    await staged.Update(
        results.ToPromote.Select(e => e.Staged.Promote(start))
            .Concat(results.ToIgnore.Select(e => e.Staged.Ignore(e.Reason)))
            .ToList());
    
    return results; 
  }
  
  private async Task WriteEntitiesToCoreStorage(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op, List<ICoreEntity> entities) {
    var toupsert = await (await entities
        .IgnoreMultipleUpdatesToSameEntity()
        .IgnoreNonMeaninfulChanges(op.State.Object, core))
        .IgnoreEntitiesBouncingBack(entitymap, op.State.System, op.State.Object);
    
    if (!toupsert.Any()) return;
    await core.Upsert(op.State.Object, toupsert);
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op, Exception ex) => new ErrorPromoteOperationResult(EOperationAbortVote.Abort, ex);

}

public static class PromoteOperationRunnerHelperExtensions {
  /// <summary>
  /// It is possible for several changes to an entity to be staged prior to promotion.  If this happens then
  /// simply take the latest snapshot of the entity to promote as there is no benefit to promoting the same
  /// entity multiple times to only end up in the latest state anyway. 
  /// </summary>
  public static List<ICoreEntity> IgnoreMultipleUpdatesToSameEntity(this List<ICoreEntity> lst) => 
        lst.GroupBy(c => c.Id)
        .Select(g => g.OrderByDescending(c => c.SourceSystemDateUpdated).First()) 
        .ToList();
  
  /// <summary>
  /// Use checksum (if available) to make sure that we are only promoting entities where their core storage representation has
  /// meaningful changes.  This is why its important that the core storage checksum be only calculated on meaningful fields. 
  /// </summary>
  public static async Task<List<ICoreEntity>> IgnoreNonMeaninfulChanges(this List<ICoreEntity> lst, CoreEntityType obj, ICoreStorageUpserter core) {
    if (lst.All(e => String.IsNullOrWhiteSpace(e.Checksum))) return lst;
    
    var checksums = await core.GetChecksums(obj, lst);
    return lst.Where(e => String.IsNullOrWhiteSpace(e.Checksum)
        || !checksums.ContainsKey(e.Id)
        || e.Checksum != checksums[e.Id]).ToList();
  } 
  
  /// <summary>
  /// Ignore fields created in system 1, written (and hence created again) to system 2, being promoted again.  This is called a bouce back. 
  /// </summary>
  public static async Task<List<ICoreEntity>> IgnoreEntitiesBouncingBack(this List<ICoreEntity> lst, IEntityIntraSystemMappingStore entitymap, SystemName thissys, CoreEntityType obj)  {
    var ids = lst.Select(e => e.SourceId).ToList();
    var valid = (await entitymap.FilterOutBouncedBackIds(thissys, obj, ids)).ToDictionary(id => id);
    return lst.Where(e => valid.ContainsKey(e.SourceId)).ToList();
  }
} 