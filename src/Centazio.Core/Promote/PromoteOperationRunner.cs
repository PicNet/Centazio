using System.Text.Json;
using Centazio.Core.Checksum;
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
    ICoreToSystemMapStore entitymap) : IOperationRunner<PromoteOperationConfig, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig> op) {
    var start = UtcDate.UtcNow;
    var pending = await staged.GetUnpromoted(op.Checkpoint, op.State.System, op.OpConfig.ExternalEntityType);
    if (!pending.Any()) return new SuccessPromoteOperationResult([], []);
    
    var results = await op.OpConfig.EvaluateEntitiesToPromote.Evaluate(op, pending);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `EvaluateEntitiesToPromote`.  Not promoting any entities, not updating StagedEntity states.");
      return results;  
    }
    var (topromote, toignore) = (results.ToPromote, results.ToIgnore);
    
    // todo: clean bidi code, maybe here add else { IgnoreBounceBacks(); }
    // todo: if we ignore the bounce-back then we should ignore this staged entity, add 'IgnoreReason'='Bounce Back'
    if (op.OpConfig.IsBidirectional) { topromote = await IdentifyBouncedBackAndSetCorrectId(op, topromote); }
    
    Log.Information($"PromoteOperationRunner Pending[{pending.Count}] ToPromote[{topromote.Count}] ToIgnore[{toignore.Count}]");
    
    await WriteEntitiesToCoreStorage(op, topromote);
    await staged.Update(
        topromote.Select(e => e.Staged.Promote(start))
            .Concat(toignore.Select(e => e.Staged.Ignore(e.Ignore)))
            .ToList());
    
    return results; 
  }

  private async Task<List<Containers.StagedSysCore>> IdentifyBouncedBackAndSetCorrectId(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSysCore> topromote) {
    // todo: if the Id is part of the checksum then this will
    //  change the Id and result in an endless bounce back.  Need to check that Id is not
    //  part of the checksum object.
    //  Also, having Id with a public setter is not nice, how can this be avoided?
    var bounces = await GetBounceBacks(op.State.System, op.State.Object.ToCoreEntityType, topromote.Select(t => t.Core).ToList());
    if (!bounces.Any()) return topromote;
    
    var originals = await core.Get(op.State.Object.ToCoreEntityType, bounces.Select(n => n.Value.OriginalCoreId).ToList());
    var changes = new List<string>();
    bounces.ForEach(bounce => {
      var idx = topromote.FindIndex(t => t.Core.SourceId == bounce.Key);
      var e = topromote[idx].Core;
      var echecksum = op.FuncConfig.ChecksumAlgorithm.Checksum(e);
      var orige = originals.Single(e2 => e2.Id == bounce.Value.OriginalCoreId);
      var origess = orige.GetChecksumSubset();
      var originalchecksum = op.FuncConfig.ChecksumAlgorithm.Checksum(orige);
      var msg = $"{op.State.System}#Id[{e.Id}] -> OriginalCoreId[{bounce.Value.OriginalCoreId}]";
      (e.Id, e.SourceId) = (bounce.Value.OriginalCoreId, bounce.Value.OriginalSourceId);
      var e2ss = e.GetChecksumSubset();
      var newchecksum = op.FuncConfig.ChecksumAlgorithm.Checksum(e);
      if (echecksum != newchecksum) throw new Exception($"Bounce-back identified and after correcting Ids we have a different checksum.  The GetChecksumSubset() method of ICoreEntity should not include Ids, updated dates or any other non-meaninful data.");
      
      changes.Add(msg + $":\n\tOriginal CS[{originalchecksum}] - {JsonSerializer.Serialize(e2ss)}\n\tNew Checksum[{newchecksum}] - {JsonSerializer.Serialize(origess)}");
      topromote[idx] = topromote[idx] with { Core = e };
    });
    if (changes.Any()) Log.Information("identified bounce-backs({@ChangesCount}) [{@ExternalSystem}/{@CoreEntityType}]\n\t" + String.Join("\n\t", changes), changes.Count, op.State.System, op.State.Object.ToCoreEntityType);
    return topromote;
  }
  
  private async Task<Dictionary<string, (string OriginalCoreId, string OriginalSourceId)>> GetBounceBacks(SystemName system, CoreEntityType coretype, List<ICoreEntity> potentialDups) {
    var maps = await entitymap.GetPreExistingSourceIdToCoreIdMap(potentialDups, system);
    if (!maps.Any()) return new();
    
    var existing = await core.Get(coretype, maps.Values.ToList());
    return maps.ToDictionary(sid_id => sid_id.Key, sid_id => {
      var preexisting = existing.Single(c => c.Id == sid_id.Value);
      return (preexisting.Id, preexisting.SourceId);
    });
  }

  private async Task WriteEntitiesToCoreStorage(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSysCore> entities) {
    var nodups = entities.IgnoreMultipleUpdatesToSameEntity();
    var nobounces = op.OpConfig.IsBidirectional ? nodups : await nodups.IgnoreEntitiesBouncingBack(op.State.System);
    var meaningful = await nobounces.IgnoreNonMeaninfulChanges(op.State.Object.ToCoreEntityType, core, op.FuncConfig.ChecksumAlgorithm.Checksum);
    Log.Information($"[{op.State.System}/{op.State.Object}] Bidi[{op.OpConfig.IsBidirectional}] Initial[{entities.Count}] No Duplicates[{nodups.Count}] No Bounces[{nobounces.Count}] Meaningful[{meaningful.Count}]");
    if (!meaningful.Any()) return;
    
    await core.Upsert(
        op.State.Object.ToCoreEntityType, 
        meaningful.ToCore().Select(
            e => new Containers.CoreChecksum(e, op.FuncConfig.ChecksumAlgorithm.Checksum(e))).ToList());
    
    var existing = await entitymap.GetNewAndExistingMappingsFromCores(meaningful.ToCore(), op.State.System);
    await entitymap.Create(op.State.Object.ToCoreEntityType, op.State.System, existing.Created.Select(e => e.Map.SuccessCreate(e.Core.SourceId, SysChecksum(e.Core))).ToList());
    await entitymap.Update(op.State.Object.ToCoreEntityType, op.State.System, existing.Updated.Select(e => e.Map.SuccessUpdate(SysChecksum(e.Core))).ToList());
    
    SystemEntityChecksum SysChecksum(ICoreEntity e) => op.FuncConfig.ChecksumAlgorithm.Checksum(meaningful.Single(c => c.Core.Id == e.Id).Sys);
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(EOperationAbortVote.Abort, ex);

}

public static class PromoteOperationRunnerHelperExtensions {
  /// <summary>
  /// It is possible for several changes to an entity to be staged prior to promotion.  If this happens then
  /// simply take the latest snapshot of the entity to promote as there is no benefit to promoting the same
  /// entity multiple times to only end up in the latest state anyway.
  /// </summary>
  public static List<Containers.StagedSysCore> IgnoreMultipleUpdatesToSameEntity(this List<Containers.StagedSysCore> lst) => 
        lst.GroupBy(c => c.Core.SourceId)
        .Select(g => g.OrderByDescending(c => c.Core.SourceSystemDateUpdated).First()) 
        .ToList();
  
  /// <summary>
  /// Use checksum (if available) to make sure that we are only promoting entities where their core storage representation has
  /// meaningful changes.  This is why its important that the core storage checksum be only calculated on meaningful fields. 
  /// </summary>
  public static async Task<List<Containers.StagedSysCore>> IgnoreNonMeaninfulChanges(this List<Containers.StagedSysCore> lst, CoreEntityType obj, ICoreStorageUpserter core, Func<ICoreEntity, CoreEntityChecksum> checksum) {
    var checksums = await core.GetChecksums(obj, lst.ToCore());
    return lst.Where(e => {
      if (!checksums.TryGetValue(e.Core.Id, out var existing)) return true;
      var newchecksum = checksum(e.Core); 
      return String.IsNullOrWhiteSpace(newchecksum) || newchecksum != existing;
    }).ToList();
  } 
  
  /// <summary>
  /// Ignore fields created in system 1, written (and hence created again) to system 2, being promoted again.  This is called a bouce back.
  ///
  /// Bounce backs are avoided by filtering out any core entities being promoted whose `SourceSystem` is not the current functions
  /// `System`.  As there should only be one source of truth system.
  ///
  /// Note: This is only done with Uni-directional promote operations. Bi-directional operations must manage their own bounce backs for now. 
  /// </summary>
  public static Task<List<Containers.StagedSysCore>> IgnoreEntitiesBouncingBack(this List<Containers.StagedSysCore> lst, SystemName thissys)  {
    return Task.FromResult(lst.Where(e => e.Core.SourceSystem == thissys).ToList());
  }
} 