﻿using Centazio.Core.Checksum;
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
    var pending = await staged.GetUnpromoted(op.Checkpoint, op.State.System, op.OpConfig.SystemEntityType);
    if (!pending.Any()) return new SuccessPromoteOperationResult([], []);
    
    var results = await op.OpConfig.EvaluateEntitiesToPromote.Evaluate(op, pending);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `EvaluateEntitiesToPromote`.  Not promoting any entities, not updating StagedEntity states.");
      return results;  
    }
    var (topromote, toignore) = (results.ToPromote, results.ToIgnore);
    var meaningful = IgnoreMultipleUpdatesToSameEntity(topromote);
    
    if (op.OpConfig.IsBidirectional) { meaningful = await IdentifyBouncedBackAndSetCorrectId(op, meaningful); }
    else meaningful = IgnoreEntitiesBouncingBack(meaningful, op.State.System);

    meaningful = await IgnoreNonMeaninfulChanges(meaningful, op.State.Object.ToCoreEntityType, core, op.FuncConfig.ChecksumAlgorithm.Checksum);
    
    var meaningulstr = String.Join(",", meaningful.Select(e => e.Core.DisplayName));
    Log.Information($"PromoteOperationRunner[{op.State.System}/{op.State.Object}] Bidi[{op.OpConfig.IsBidirectional}] Pending[{pending.Count}] ToPromote[{topromote.Count}] Meaningful[{meaningful.Count}({meaningulstr})] ToIgnore[{toignore.Count}]");
    
    await WriteEntitiesToCoreStorageAndUpdateMaps(op, meaningful);
    await staged.Update(
        // mark all StagedEntities as promoted, even if they were ignored above
        topromote.Select(e => e.Staged.Promote(start))
            .Concat(toignore.Select(e => e.Staged.Ignore(e.Ignore)))
            .ToList());
    
    return results; 
  }

  private async Task<List<Containers.StagedSysCore>> IdentifyBouncedBackAndSetCorrectId(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSysCore> topromote) {
    var bounces = await GetBounceBacks(op.State.System, op.State.Object.ToCoreEntityType, topromote.Select(t => t.Core).ToList());
    if (!bounces.Any()) return topromote;
    
    var originals = await core.Get(op.State.Object.ToCoreEntityType, bounces.Select(n => n.Value.OriginalCoreId).ToList());
    var entities = bounces.Select(b => {
      var idx = topromote.FindIndex(t => t.Core.SourceId == b.Key);
      var original = originals.Single(e2 => e2.Id == b.Value.OriginalCoreId);
      return (
          SourceId: b.Key, 
          b.Value.OriginalSourceId, 
          b.Value.OriginalCoreId, 
          OriginalEntity: original,
          OriginalEntityChecksum: Checksum(original),
          OriginalEntityChecksumSubset: original.GetChecksumSubset(),
          ToPromoteIdx: idx,
          ToPromoteCore: topromote[idx].Core,
          PreChangeChecksumSubset: topromote[idx].Core.GetChecksumSubset(),
          PreChangeChecksum: Checksum(topromote[idx].Core),
          PostChangeChecksumSubset: new object(),
          PostChangeChecksum: String.Empty);
    }).ToList();
    
    var updated = entities.Select(e => {
      // If the entity is bouncing back, it will have a new Core and SourceId (as it would have been created in the second system).
      //    We need to correct the process here and make it point to the original Core/Source Id as we do not want
      //    multiple core records for the same entity.
      (e.ToPromoteCore.Id, e.ToPromoteCore.SourceId) = (e.OriginalCoreId, e.OriginalSourceId);
      topromote[e.ToPromoteIdx] = topromote[e.ToPromoteIdx] with { Core = e.ToPromoteCore };
      e.PostChangeChecksumSubset = e.ToPromoteCore.GetChecksumSubset();
      e.PostChangeChecksum = Checksum(e.ToPromoteCore);
      return e;
    }).ToList();
    
    var msgs = updated.Select(e => $"[{e.ToPromoteCore.DisplayName}({e.ToPromoteCore.Id})] -> OriginalCoreId[{e.OriginalCoreId}] Meaningful[{e.OriginalEntityChecksum != e.PostChangeChecksum}]:" +
          $"\n\tOriginal Checksum[{e.OriginalEntityChecksumSubset}({e.OriginalEntityChecksum})]" +
          $"\n\tNew Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]");
    Log.Information("PromoteOperationRunner: identified bounce-backs({@ChangesCount}) [{@System}/{@CoreEntityType}]" + String.Join("\n", msgs), bounces.Count, op.State.System, op.State.Object.ToCoreEntityType);
    
    var errs = updated.Where(e => e.PreChangeChecksum != e.PostChangeChecksum)
        .Select(e => $"\n[{e.ToPromoteCore.DisplayName}({e.ToPromoteCore.Id})]:" +
          $"\n\tPrechange Checksum[{e.PreChangeChecksumSubset}({e.PreChangeChecksum})]" +
          $"\n\tPostchange Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]")
        .ToList();
    if (errs.Any())
      throw new Exception($"Bounce-back identified and after correcting ids we got a different checksum.  " +
            $"\nThe GetChecksumSubset() method of ICoreEntity should not include Ids, updated dates or any other non-meaninful data." +
            $"\nHaving the Id as part of the ChecksumSubset will mean that we will get infinite bounce backs between two systems." + errs);
    
    return topromote;
    
    string Checksum(ICoreEntity c) => op.FuncConfig.ChecksumAlgorithm.Checksum(c);
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

  private async Task WriteEntitiesToCoreStorageAndUpdateMaps(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSysCore> entities) {
    if (!entities.Any()) return;
    await core.Upsert(
        op.State.Object.ToCoreEntityType, 
        entities.ToCore().Select(
            e => new Containers.CoreChecksum(e, op.FuncConfig.ChecksumAlgorithm.Checksum(e))).ToList());
    
    var existing = await entitymap.GetNewAndExistingMappingsFromCores(entities.ToCore(), op.State.System);
    await entitymap.Create(op.State.Object.ToCoreEntityType, op.State.System, existing.Created.Select(e => e.Map.SuccessCreate(e.Core.SourceId, SysChecksum(e.Core))).ToList());
    await entitymap.Update(op.State.Object.ToCoreEntityType, op.State.System, existing.Updated.Select(e => e.Map.SuccessUpdate(SysChecksum(e.Core))).ToList());
    
    SystemEntityChecksum SysChecksum(ICoreEntity e) => op.FuncConfig.ChecksumAlgorithm.Checksum(entities.Single(c => c.Core.Id == e.Id).Sys);
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(EOperationAbortVote.Abort, ex);

  /// <summary>
  /// It is possible for several changes to an entity to be staged prior to promotion.  If this happens then
  /// simply take the latest snapshot of the entity to promote as there is no benefit to promoting the same
  /// entity multiple times to only end up in the latest state anyway.
  /// </summary>
  internal static List<Containers.StagedSysCore> IgnoreMultipleUpdatesToSameEntity(List<Containers.StagedSysCore> lst) => 
        lst.GroupBy(c => c.Core.SourceId)
        .Select(g => g.OrderByDescending(c => c.Core.SourceSystemDateUpdated).First()) 
        .ToList();
  
  /// <summary>
  /// Use checksum (if available) to make sure that we are only promoting entities where their core storage representation has
  /// meaningful changes.  This is why its important that the core storage checksum be only calculated on meaningful fields. 
  /// </summary>
  internal static async Task<List<Containers.StagedSysCore>> IgnoreNonMeaninfulChanges(List<Containers.StagedSysCore> lst, CoreEntityType obj, ICoreStorageUpserter core, Func<ICoreEntity, CoreEntityChecksum> checksum) {
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
  internal static List<Containers.StagedSysCore> IgnoreEntitiesBouncingBack(List<Containers.StagedSysCore> lst, SystemName thissys)  {
    return lst.Where(e => e.Core.SourceSystem == thissys).ToList();
  }
}