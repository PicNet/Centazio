using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Promote;

public class PromotionBag(StagedEntity staged) {
  public StagedEntity StagedEntity { get; init; } = staged;
  public ISystemEntity Sys { get; set; } = null!;
  public ICoreEntity? ExistingCoreEntity { get; set; }
  public ICoreEntity? UpdatedCoreEntity { get; set; }
  public CoreEntityChecksum? UpdatedCoreEntityChecksum { get; set; }
  public ValidString? IgnoreReason { get; set; }
  
  public void MarkIgnore(ValidString reason) {
    UpdatedCoreEntity = null;
    IgnoreReason = reason;
  } 
      
  public bool IsCreating => ExistingCoreEntity is null;
  public bool IsIgnore => IgnoreReason is not null;
}

public record EntityForPromotionEvaluation(ISystemEntity SysEnt, ICoreEntity? ExistingCoreEntity) {
  public EntityEvaluationResult MarkForPromotion(ICoreEntity updated) => new EntityToPromote(SysEnt, updated);
  public EntityEvaluationResult MarkForIgnore(ValidString reason) => new EntityToIgnore(SysEnt, reason);
}

public abstract record EntityEvaluationResult(ISystemEntity SysEnt);
public sealed record EntityToPromote(ISystemEntity SysEnt, ICoreEntity UpdatedEntity) : EntityEvaluationResult(SysEnt);
public sealed record EntityToIgnore(ISystemEntity SysEnt, ValidString IgnoreReason) : EntityEvaluationResult(SysEnt);

public class PromotionSteps(ICoreStorage core, ICoreToSystemMapStore entitymap, OperationStateAndConfig<PromoteOperationConfig> op) {

  private readonly CoreEntityTypeName corename = op.State.Object.ToCoreEntityTypeName;
  private readonly DateTime start = UtcDate.UtcNow;
  
  private List<PromotionBag> bags = [];
  
  public async Task LoadPendingStagedEntities(IStagedEntityStore stagestore) {
    var staged = await stagestore.GetUnpromoted(op.State.System, op.OpConfig.SystemEntityTypeName, op.Checkpoint);
    bags = staged.Select(se => new PromotionBag(se)).ToList();
  }
  
  public void DeserialisePendingStagedEntities() {
    if (IsEmpty()) return;
    bags.ForEach(bag => bag.Sys = (ISystemEntity) Json.Deserialize(bag.StagedEntity.Data, op.OpConfig.SystemEntityType));
  }
  
  public async Task LoadExistingCoreEntities() {
    if (IsEmpty()) return;
    var sysids = bags.Select(bag => bag.Sys.SystemId).ToList();
    var maps = await entitymap.GetExistingMappingsFromSystemIds(op.State.System, op.OpConfig.CoreEntityTypeName, sysids);
    var coreids = maps.Select(m => m.CoreId).ToList();
    var coreents = await core.Get(op.OpConfig.CoreEntityTypeName, coreids);
    bags.ForEach(bag => {
      var coreid = maps.SingleOrDefault(m => m.SystemId == bag.Sys.SystemId)?.CoreId;
      bag.ExistingCoreEntity = coreid is null ? null : coreents.Single(e => e.CoreId == coreid);
    });
  }
  
  public async Task ApplyChangesToCoreEntities() {
    if (IsEmpty()) return;
    var list = await op.OpConfig.PromoteEvaluator.BuildCoreEntities(op, bags.Select(bag => new EntityForPromotionEvaluation(bag.Sys, bag.ExistingCoreEntity)).ToList());
    list.ForEach(result => {
      var bag = bags.Single(b => b.Sys.SystemId == result.SysEnt.SystemId);
      if (result is EntityToPromote topromote) bag.UpdatedCoreEntity = CheckAndSetInternalState(topromote.UpdatedEntity);
      else if (result is EntityToIgnore toignore) bag.MarkIgnore(toignore.IgnoreReason);
      else throw new NotSupportedException(result.GetType().Name);
      
      ICoreEntity CheckAndSetInternalState(ICoreEntity e) {
        if (bag.Sys.SystemId != e.SystemId) throw new Exception();
        
        e.DateUpdated = UtcDate.UtcNow;
        e.LastUpdateSystem = op.State.System;
        if (!bag.IsCreating) return e;

        e.DateCreated = UtcDate.UtcNow;
        e.System = op.State.System;
        return e;
      }
    });
  }
  
  public void IgnoreUpdatesToSampleEntityInBatch() {
    if (IsEmpty()) return;
    var added = new Dictionary<SystemEntityId, bool>();
    bags = bags.OrderByDescending(bag => bag.Sys.LastUpdatedDate).ToList(); 
    bags.ForEach(bag => {
      if (bag.IsIgnore) return;
      if (!added.TryAdd(bag.Sys.SystemId, true)) bag.MarkIgnore("already added in batch, taking most recently updated");
    });
  }
  
  public async Task HandleEntitiesBouncingBack() {
    if (IsEmpty()) return;
    
    if (!op.OpConfig.IsBidirectional) { 
      IgnoreEntitiesBouncingBack();
      return;
    }
    
    await IdentifyBouncedBackAndSetCorrectId();
    
    async Task IdentifyBouncedBackAndSetCorrectId() {
      var topromote = ToPromote();
      var bounces = await GetBounceBacks();
      if (!bounces.Any()) return;
      
      var toupdate = bounces.Select(bounce => {
        var idx = topromote.FindIndex(bag => bag.Sys.SystemId == bounce.SystemId);
        var original = bounce.OriginalCoreEntity;
        return (
            bounce.SystemId, 
            OriginalEntity: original,
            OriginalEntityChecksum: op.FuncConfig.ChecksumAlgorithm.Checksum(original),
            OriginalEntityChecksumSubset: original.GetChecksumSubset(),
            ToPromoteIdx: idx,
            ToPromoteCore: topromote[idx].UpdatedCoreEntity!,
            PreChangeChecksumSubset: topromote[idx].UpdatedCoreEntity!.GetChecksumSubset(),
            PreChangeChecksum: op.FuncConfig.ChecksumAlgorithm.Checksum(topromote[idx].UpdatedCoreEntity!),
            PostChangeChecksumSubset: new object(),
            PostChangeChecksum: String.Empty);
      }).ToList();
      
      var updated = toupdate.Select(e => {
        // If the entity is bouncing back, it will have a new Core and SystemId (as it would have been created in the second system).
        //    We need to correct the process here and make it point to the original Core/System Id as we do not want
        //    multiple core records for the same entity.
        (e.ToPromoteCore.CoreId, e.ToPromoteCore.SystemId) = (e.OriginalEntity.CoreId, e.OriginalEntity.SystemId);
        topromote[e.ToPromoteIdx].UpdatedCoreEntity = e.OriginalEntity;
        e.PostChangeChecksumSubset = e.ToPromoteCore.GetChecksumSubset();
        e.PostChangeChecksum = op.FuncConfig.ChecksumAlgorithm.Checksum(e.ToPromoteCore);
        return e;
      }).ToList();
      
      var msgs = updated.Select(e => $"[{e.ToPromoteCore.DisplayName}({e.ToPromoteCore.CoreId})] -> OriginalCoreId[{e.OriginalEntity.CoreId}] Meaningful[{e.OriginalEntityChecksum != e.PostChangeChecksum}]:" +
            $"\n\tOriginal Checksum[{e.OriginalEntityChecksumSubset}({e.OriginalEntityChecksum})]" +
            $"\n\tNew Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]");
      Log.Information("PromoteOperationRunner: identified bounce-backs({@ChangesCount}) [{@System}/{@CoreEntityTypeName}]" + String.Join("\n", msgs), bounces.Count, op.State.System, corename);
      
      var errs = updated.Where(e => e.PreChangeChecksum != e.PostChangeChecksum)
          .Select(e => $"\n[{e.ToPromoteCore.DisplayName}({e.ToPromoteCore.CoreId})]:" +
            $"\n\tPrechange Checksum[{e.PreChangeChecksumSubset}({e.PreChangeChecksum})]" +
            $"\n\tPostchange Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]")
          .ToList();
      if (errs.Any())
        throw new Exception($"Bounce-back identified and after correcting ids we got a different checksum.  " +
              $"\nThe GetChecksumSubset() method of ICoreEntity should not include Ids, updated dates or any other non-meaninful data." +
              $"\nHaving the Id as part of the ChecksumSubset will mean that we will get infinite bounce backs between two systems." + errs);
      
     
      async Task<List<(SystemEntityId SystemId, ICoreEntity OriginalCoreEntity)>> GetBounceBacks() {
        var maps = await entitymap.GetPreExistingSystemIdToCoreIdMap(op.State.System, corename, topromote.Select(bag => bag.UpdatedCoreEntity!).ToList());
        if (!maps.Any()) return new();
        
        var existing = await core.Get(corename, maps.Values.ToList());
        return maps.Select(kvp => (kvp.Key, existing.Single(e => e.CoreId == kvp.Value))).ToList();
      }
    }
    
    void IgnoreEntitiesBouncingBack() {
      bags
          .Where(bag => !bag.IsIgnore && bag.UpdatedCoreEntity!.SystemId != op.State.System)
          .ForEach(bag => bag.MarkIgnore("update is a bounce-back and entity is not bi-directional"));
    }
  }
  
  public async Task IgnoreNonMeaninfulChanges() {
    if (IsEmpty()) return;
    var topromote = ToPromote();
    // todo: dont we have OriginalChecksum from when we loaded exsiting entities at top of this?
    var checksums = await core.GetChecksums(corename, topromote.Select(bag => bag.UpdatedCoreEntity!.CoreId).ToList());
    var toignore = topromote.Where(bag => {
      if (!checksums.TryGetValue(bag.UpdatedCoreEntity!.CoreId, out var existing)) return false;
      bag.UpdatedCoreEntityChecksum = op.FuncConfig.ChecksumAlgorithm.Checksum(bag.UpdatedCoreEntity); 
      var ignore = !String.IsNullOrWhiteSpace(bag.UpdatedCoreEntityChecksum) && 
          !String.IsNullOrWhiteSpace(existing) && 
          bag.UpdatedCoreEntityChecksum == existing;
      return ignore;
    }).ToList();
    toignore.ForEach(bag => bag.MarkIgnore("no meaningful change detected on entity"));
  } 
  
  public async Task WriteEntitiesToCoreStorageAndUpdateMaps() {
    if (IsEmpty()) return;
    var topromote = ToPromote();
    await core.Upsert(op.State.Object.ToCoreEntityTypeName, topromote.Select(bag => (bag.UpdatedCoreEntity!, bag.UpdatedCoreEntityChecksum!)).ToList());
    
    var existing = await entitymap.GetNewAndExistingMappingsFromCores(op.State.System, topromote.Select(bag => bag.UpdatedCoreEntity!).ToList());
    await entitymap.Create(op.State.System, op.State.Object.ToCoreEntityTypeName, existing.Created.Select(e => e.Map.SuccessCreate(e.Core.SystemId, SysChecksum(e.Core))).ToList());
    await entitymap.Update(op.State.System, op.State.Object.ToCoreEntityTypeName, existing.Updated.Select(e => e.Map.SuccessUpdate(SysChecksum(e.Core))).ToList());
    
    SystemEntityChecksum SysChecksum(ICoreEntity e) => op.FuncConfig.ChecksumAlgorithm.Checksum(topromote.Single(c => c.UpdatedCoreEntity!.CoreId == e.CoreId).Sys);
  }
  
  public async Task UpdateAllStagedEntitiesWithNewState(IStagedEntityStore stagestore) => 
      await stagestore.Update(bags.Select(bag => bag.IsIgnore ? bag.StagedEntity.Ignore(bag.IgnoreReason!) : bag.StagedEntity.Promote(start)).ToList());
  
  public bool IsEmpty() => bags.All(bag => bag.IsIgnore);
  public List<PromotionBag> ToPromote() => bags.Where(bag => !bag.IsIgnore).ToList();
}