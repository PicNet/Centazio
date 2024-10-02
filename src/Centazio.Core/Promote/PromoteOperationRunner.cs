using System.Text.Json;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Promote;

public class PromoteOperationRunner(
    IStagedEntityStore staged,
    ICoreStorage core,
    ICoreToSystemMapStore entitymap) : IOperationRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op) {
    var start = UtcDate.UtcNow;
    var pending = await staged.GetUnpromoted(op.Checkpoint, op.State.System, op.Config.ExternalEntityType);
    if (!pending.Any()) return new SuccessPromoteOperationResult([], []);
    
    var results = await op.Config.EvaluateEntitiesToPromote.Evaluate(op, pending);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `EvaluateEntitiesToPromote`.  Not promoting any entities, not updating StagedEntity states.");
      return results;  
    }
    var (topromote, toignore) = (results.ToPromote, results.ToIgnore);
    
    if (op.Config.IsBidirectional) { topromote = await IdentifyBouncedBackAndSetCorrectId(op.State.System, op.State.Object, topromote); }
    
    Log.Information($"PromoteOperationRunner Pending[{pending.Count}] ToPromote[{topromote.Count}] ToIgnore[{toignore.Count}]");
    
    await WriteEntitiesToCoreStorage(op, topromote.Select(p => p.Core).ToList());
    await staged.Update(
        topromote.Select(e => e.Staged.Promote(start))
            .Concat(toignore.Select(e => e.Staged.Ignore(e.Reason)))
            .ToList());
    
    return results; 
  }

  private async Task<List<StagedAndCoreEntity>> IdentifyBouncedBackAndSetCorrectId(SystemName system, CoreEntityType coretype, List<StagedAndCoreEntity> topromote) {
    // todo: if the Id is part of the checksum then this will
    //  change the Id and result in an endless bounce back.  Need to check that Id is not
    //  part of the checksum object.
    //  Also, having Id with a public setter is not nice, how can this be avoided?
    var bounces = await GetBounceBacks(system, coretype, topromote.Select(t => t.Core).ToList());
    if (!bounces.Any()) return topromote;
    
    var changes = new List<string>();
    bounces.ForEach(bounce => {
      var idx = topromote.FindIndex(t => t.Core.SourceId == bounce.Key);
      var e = topromote[idx].Core;
      changes.Add($"{e.Id}->{bounce.Value.NewId}");
      (e.Id, e.SourceId) = (bounce.Value.NewId, bounce.Value.NewSourceId);
      topromote[idx] = topromote[idx] with { Core = e };
    });
    if (changes.Any()) Log.Information("identified bounce backs {@ExternalSystem} {@CoreEntityType} {@Changes}", system, coretype, changes);
    return topromote;
  }
  
  private async Task<Dictionary<string, (string NewId, string NewSourceId)>> GetBounceBacks(SystemName system, CoreEntityType coretype, List<ICoreEntity> potentialDups) {
    var maps = await entitymap.GetPreExistingCoreIds(potentialDups, system);
    if (!maps.Any()) return new();
    
    var existing = await core.Get(coretype, maps.Values.ToList());
    return maps.ToDictionary(sid_id => sid_id.Key, sid_id => {
      var preexisting = existing.Single(c => c.Id == sid_id.Value);
      return (preexisting.Id, preexisting.SourceId);
    });
  }

  private async Task WriteEntitiesToCoreStorage(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op, List<ICoreEntity> entities) {
    var nodups = entities.IgnoreMultipleUpdatesToSameEntity();
    var meaningful = await nodups.IgnoreNonMeaninfulChanges(op.State.Object, core);
    var toupsert = op.Config.IsBidirectional ? meaningful : await meaningful.IgnoreEntitiesBouncingBack(op.State.System);
    Log.Information($"[{op.State.System}/{op.State.Object}] Initial[{entities.Count}] No Duplicates[{nodups.Count}] Meaningful[{meaningful.Count}] Upserting[{toupsert.Count}]");
    if (!toupsert.Any()) return;
    
    await core.Upsert(op.State.Object, toupsert);
    
    var existing = await entitymap.GetForCores(toupsert, op.State.System); 
    await entitymap.Create(existing.Created.Select(e => e.Map.SuccessCreate(e.Core.SourceId)).ToList());
    await entitymap.Update(existing.Updated.Select(e => e.Map.SuccessUpdate()).ToList());
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
  ///
  /// Bounce backs are avoided by filtering out any core entities being promoted whose `SourceSystem` is not the current functions
  /// `System`.  As there should only be one source of truth system.
  ///
  /// Note: This is only done with Uni-directional promote operations. Bi-directional operations must manage their own bounce backs for now. 
  /// </summary>
  public static Task<List<ICoreEntity>> IgnoreEntitiesBouncingBack(this List<ICoreEntity> lst, SystemName thissys)  {
    return Task.FromResult(lst.Where(e => e.SourceSystem == thissys).ToList());
  }
} 