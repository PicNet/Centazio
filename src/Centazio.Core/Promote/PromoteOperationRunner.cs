using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

internal class PromoteOperationRunner(IStagedEntityStore staged, ICoreStorageUpserter core) 
    : IOperationRunner<PromoteOperationConfig, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<PromoteOperationConfig> op) {
    var pending = await staged.Get(op.Checkpoint, op.State.System, op.State.Object);
    var results = await op.Settings.EvaluateEntitiesToPromote(op, pending);
    
    var promote = results.ToPromote.ToList();
    var ignore = results.ToIgnore.ToList();
    
    if (promote.Any()) await WriteEntitiesToCoreStorage(promote.Select(p => p.Core).ToList());
    
    await staged.Update(
        promote.Select(e => e.Staged with { DatePromoted = funcstart }).Concat(
            ignore.Select(e => e.Entity with { Ignore = e.Reason })));
    
    return results; 
  }

  private async Task WriteEntitiesToCoreStorage(List<ICoreEntity> entities) {
    var toupsert = await (await entities
        .IgnoreMultipleUpdatesToSameEntity()
        .IgnoreNonMeaninfulChanges(core))
        .IgnoreEntitiesBouncingBack(core);
    
    if (!toupsert.Any()) return;
    await core.Upsert(toupsert);
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(ex.Message, EOperationAbortVote.Abort, ex);

}

public static class PromoteOperationRunnerHelperExtensions {
  public static List<T> IgnoreMultipleUpdatesToSameEntity<T>(this List<T> lst) where T : ICoreEntity => 
        lst.GroupBy(c => c.Id)
        // take latest only
        .Select(g => g.OrderByDescending(c => c.SourceSystemDateUpdated).First()) 
        .ToList();
  
  public static async Task<List<T>> IgnoreNonMeaninfulChanges<T>(this List<T> lst, ICoreStorageUpserter core) where T : ICoreEntity {
    var checksums = await core.GetChecksums(lst);
    return lst.Where(e => String.IsNullOrWhiteSpace(e.Checksum)
        || !checksums.ContainsKey(e.Id)
        || e.Checksum != checksums[e.Id]).ToList();
  } 
  
  public static Task<List<T>> IgnoreEntitiesBouncingBack<T>(this List<T> lst, ICoreStorageUpserter core) where T : ICoreEntity {
    // todo: when an entity is created in sys1, it will be promoted to core storage.  This entity can then be
    //  written (and hence created again) in another system, say sys2.  Now, since its created in sys2 the entity
    //  will try to be promoted again here.  It needs to be ignored.
    /*
      var externalids = db.TargetSystemEntity.
          Where(tse => tse.CoreEntity == typeof(C).Name && tse.TargetSystem == state.System && tse.TargetPk != null && ids.Contains(tse.TargetPk)).
          Select(tse => tse.TargetPk!).
          ToList();
       */
    return Task.FromResult(lst);
  }
}