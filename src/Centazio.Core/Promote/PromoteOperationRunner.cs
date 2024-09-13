using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

internal class PromoteOperationRunner<C>(
    IStagedEntityStore staged, 
    IEntityIntraSystemMappingStore entitymap,
    ICoreStorageUpserter core) : IOperationRunner<PromoteOperationConfig<C>, PromoteOperationResult<C>> where C : ICoreEntity {
  
  public async Task<PromoteOperationResult<C>> RunOperation(OperationStateAndConfig<PromoteOperationConfig<C>> op) {
    var start = UtcDate.UtcNow;
    var pending = await staged.GetUnpromoted(op.Checkpoint, op.State.System, op.State.Object);
    var results = await op.Settings.EvaluateEntitiesToPromote(op, pending);
    var (promote, ignore) = (results.ToPromote.ToList(), results.ToIgnore.ToList());
    if (promote.Any()) await WriteEntitiesToCoreStorage(op, promote.Select(p => p.Core).ToList());
    
    await staged.Update(
        promote.Select(e => e.Staged with { DatePromoted = start }).Concat(
            ignore.Select(e => e.Entity with { Ignore = e.Reason })));
    
    return results; 
  }

  private async Task WriteEntitiesToCoreStorage<T>(OperationStateAndConfig<PromoteOperationConfig<C>> op, List<T> entities) where T : ICoreEntity {
    var toupsert = await (await entities
        .IgnoreMultipleUpdatesToSameEntity()
        .IgnoreNonMeaninfulChanges(core))
        .IgnoreEntitiesBouncingBack(entitymap, op.State.System);
    
    if (!toupsert.Any()) return;
    await core.Upsert(toupsert);
  }

  public PromoteOperationResult<C> BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig<C>> op, Exception ex) => new ErrorPromoteOperationResult<C>(ex.Message, EOperationAbortVote.Abort, ex);

}

public static class PromoteOperationRunnerHelperExtensions {
  /// <summary>
  /// It is possible for several changes to an entity to be staged prior to promotion.  If this happens then
  /// simply take the latest snapshot of the entity to promote as there is no benefit to promoting the same
  /// entity multiple times to only end up in the latest state anyway. 
  /// </summary>
  public static List<T> IgnoreMultipleUpdatesToSameEntity<T>(this List<T> lst) where T : ICoreEntity => 
        lst.GroupBy(c => c.Id)
        .Select(g => g.OrderByDescending(c => c.SourceSystemDateUpdated).First()) 
        .ToList();
  
  /// <summary>
  /// Use checksum (if available) to make sure that we are only promoting entities where their core storage representation has
  /// meaningful changes.  This is why its important that the core storage checksum be only calculated on meaningful fields. 
  /// </summary>
  public static async Task<List<T>> IgnoreNonMeaninfulChanges<T>(this List<T> lst, ICoreStorageUpserter core) where T : ICoreEntity {
    if (lst.All(e => String.IsNullOrWhiteSpace(e.Checksum))) return lst;
    
    var checksums = await core.GetChecksums(lst);
    return lst.Where(e => String.IsNullOrWhiteSpace(e.Checksum)
        || !checksums.ContainsKey(e.Id)
        || e.Checksum != checksums[e.Id]).ToList();
  } 
  
  /// <summary>
  /// Ignore fields created in system 1, written (and hence created again) to system 2, being promoted again.  This is called a bouce back. 
  /// </summary>
  public static async Task<List<T>> IgnoreEntitiesBouncingBack<T>(this List<T> lst, IEntityIntraSystemMappingStore entitymap, SystemName thissys) where T : ICoreEntity {
    var ids = lst.Select(e => e.SourceId).ToList();
    var valid = (await entitymap.FilterOutBouncedBackIds<T>(thissys, ids)).ToDictionary(id => id);
    return lst.Where(e => valid.ContainsKey(e.SourceId)).ToList();
  }
} 