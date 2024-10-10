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
    var pending = await staged.GetUnpromoted(op.State.System, op.OpConfig.SystemEntityType, op.Checkpoint);
    if (!pending.Any()) return new SuccessPromoteOperationResult([], []);
    
    var sysents = op.OpConfig.PromoteEvaluator.DeserialiseStagedEntities(op, pending);
    var syscores = await GetExistingCoreEntitiesForSysEnts(op, sysents);
    var results = await op.OpConfig.PromoteEvaluator.BuildCoreEntities(op, syscores);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning($"error occurred calling `EvaluateEntitiesToPromote`.  Not promoting any entities, not updating StagedEntity states.");
      return results;  
    }
    var (topromote, toignore) = (results.ToPromote, results.ToIgnore);
    var meaningful = IgnoreMultipleUpdatesToSameEntity(topromote);
    
    if (op.OpConfig.IsBidirectional) { meaningful = await IdentifyBouncedBackAndSetCorrectId(op, meaningful); }
    else meaningful = IgnoreEntitiesBouncingBack(meaningful, op.State.System);

    meaningful = await IgnoreNonMeaninfulChanges(meaningful, op.State.Object.ToCoreEntityType, core, op.FuncConfig.ChecksumAlgorithm.Checksum);
    
    Log.Information($"PromoteOperationRunner[{op.State.System}/{op.State.Object}] Bidi[{op.OpConfig.IsBidirectional}] Pending[{pending.Count}] ToPromote[{topromote.Count}] Meaningful[{meaningful.Count}] ToIgnore[{toignore.Count}]");
    
    await WriteEntitiesToCoreStorageAndUpdateMaps(op, meaningful);
    await staged.Update(
        // mark all StagedEntities as promoted, even if they were ignored above
        topromote.Select(e => e.Staged.Promote(start))
            .Concat(toignore.Select(e => e.Staged.Ignore(e.Ignore)))
            .ToList());
    
    return results; 
  }

  private async Task<List<Containers.StagedSysOptionalCore>> GetExistingCoreEntitiesForSysEnts(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSys> stagedsys) {
    var sysids = stagedsys.Select(e => e.Sys.SystemId).ToList();
    var maps = await entitymap.GetExistingMappingsFromSystemIds(op.State.System, op.OpConfig.CoreEntityType, sysids);
    var coreids = maps.Select(m => m.CoreId).ToList();
    var coreents = await core.Get(op.OpConfig.CoreEntityType, coreids);
    var syscores = stagedsys.Select(t => {
      var coreid = maps.SingleOrDefault(m => m.SystemId == t.Sys.SystemId)?.CoreId;
      var coreent = coreid is null ? null : coreents.Single(e => e.CoreId == coreid); 
      return new Containers.StagedSysOptionalCore(t.Staged, t.Sys, coreent);
    }).ToList();
    return syscores;
  }

  private async Task<List<Containers.StagedSysCore>> IdentifyBouncedBackAndSetCorrectId(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSysCore> topromote) {
    var bounces = await GetBounceBacks(op.State.System, op.State.Object.ToCoreEntityType, topromote.ToCore());
    if (!bounces.Any()) return topromote;
    
    var originals = await core.Get(op.State.Object.ToCoreEntityType, bounces.Select(n => n.Value.OriginalCoreId).ToList());
    var entities = bounces.Select(b => {
      var idx = topromote.FindIndex(t => t.Core.SystemId == b.Key);
      var original = originals.Single(e2 => e2.CoreId == b.Value.OriginalCoreId);
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
      (e.ToPromoteCore.CoreId, e.ToPromoteCore.SystemId) = (e.OriginalCoreId, e.OriginalSourceId);
      topromote[e.ToPromoteIdx] = topromote[e.ToPromoteIdx] with { Core = e.ToPromoteCore };
      e.PostChangeChecksumSubset = e.ToPromoteCore.GetChecksumSubset();
      e.PostChangeChecksum = Checksum(e.ToPromoteCore);
      return e;
    }).ToList();
    
    var msgs = updated.Select(e => $"[{e.ToPromoteCore.DisplayName}({e.ToPromoteCore.CoreId})] -> OriginalCoreId[{e.OriginalCoreId}] Meaningful[{e.OriginalEntityChecksum != e.PostChangeChecksum}]:" +
          $"\n\tOriginal Checksum[{e.OriginalEntityChecksumSubset}({e.OriginalEntityChecksum})]" +
          $"\n\tNew Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]");
    Log.Information("PromoteOperationRunner: identified bounce-backs({@ChangesCount}) [{@System}/{@CoreEntityType}]" + String.Join("\n", msgs), bounces.Count, op.State.System, op.State.Object.ToCoreEntityType);
    
    var errs = updated.Where(e => e.PreChangeChecksum != e.PostChangeChecksum)
        .Select(e => $"\n[{e.ToPromoteCore.DisplayName}({e.ToPromoteCore.CoreId})]:" +
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
  
  private async Task<Dictionary<SystemEntityId, (CoreEntityId OriginalCoreId, SystemEntityId OriginalSourceId)>> GetBounceBacks(SystemName system, CoreEntityType coretype, List<ICoreEntity> entities) {
    var maps = await entitymap.GetPreExistingSourceIdToCoreIdMap(system, coretype, entities);
    if (!maps.Any()) return new();
    
    var existing = await core.Get(coretype, maps.Values.ToList());
    return maps.ToDictionary(sid_id => sid_id.Key, sid_id => {
      var preexisting = existing.Single(c => c.CoreId == sid_id.Value);
      return (Id: preexisting.CoreId, SourceId: preexisting.SystemId);
    });
  }

  private async Task WriteEntitiesToCoreStorageAndUpdateMaps(OperationStateAndConfig<PromoteOperationConfig> op, List<Containers.StagedSysCore> entities) {
    if (!entities.Any()) return;
    await core.Upsert(
        op.State.Object.ToCoreEntityType, 
        entities.ToCore().Select(
            e => {
              e.LastUpdateSystem = op.State.System;
              return new Containers.CoreChecksum(e, op.FuncConfig.ChecksumAlgorithm.Checksum(e));
            }).ToList());
    
    var existing = await entitymap.GetNewAndExistingMappingsFromCores(op.State.System, entities.ToCore());
    await entitymap.Create(op.State.System, op.State.Object.ToCoreEntityType, existing.Created.Select(e => e.Map.SuccessCreate(e.Core.SystemId, SysChecksum(e.Core))).ToList());
    await entitymap.Update(op.State.System, op.State.Object.ToCoreEntityType, existing.Updated.Select(e => e.Map.SuccessUpdate(SysChecksum(e.Core))).ToList());
    
    SystemEntityChecksum SysChecksum(ICoreEntity e) => op.FuncConfig.ChecksumAlgorithm.Checksum(entities.Single(c => c.Core.CoreId == e.CoreId).Sys);
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(EOperationAbortVote.Abort, ex);

  /// <summary>
  /// It is possible for several changes to an entity to be staged prior to promotion.  If this happens then
  /// simply take the latest snapshot of the entity to promote as there is no benefit to promoting the same
  /// entity multiple times to only end up in the latest state anyway.
  /// </summary>
  internal static List<Containers.StagedSysCore> IgnoreMultipleUpdatesToSameEntity(List<Containers.StagedSysCore> lst) => 
        lst.GroupBy(c => c.Core.SystemId)
        .Select(g => g.OrderByDescending(c => c.Core.SourceSystemDateUpdated).First()) 
        .ToList();
  
  /// <summary>
  /// Use checksum (if available) to make sure that we are only promoting entities where their core storage representation has
  /// meaningful changes.  This is why its important that the core storage checksum be only calculated on meaningful fields. 
  /// </summary>
  internal static async Task<List<Containers.StagedSysCore>> IgnoreNonMeaninfulChanges(List<Containers.StagedSysCore> lst, CoreEntityType coretype, ICoreStorageUpserter core, Func<ICoreEntity, CoreEntityChecksum> checksum) {
    var checksums = await core.GetChecksums(coretype, lst.ToCore());
    return lst.Where(e => {
      if (!checksums.TryGetValue(e.Core.CoreId, out var existing)) return true;
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