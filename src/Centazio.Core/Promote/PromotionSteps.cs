using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Promote;

public class PromotionSteps(ICoreStorage core, ICtlRepository ctl, OperationStateAndConfig<PromoteOperationConfig> op) {

  private readonly SystemName system = op.State.System;
  private readonly CoreEntityTypeName corename = op.State.Object.ToCoreEntityTypeName;
  private readonly DateTime start = UtcDate.UtcNow;
  private Exception? error;
  internal List<PromotionBag> bags = [];
  
  public async Task LoadPendingStagedEntities(IStagedEntityRepository stagestore) {
    var staged = await stagestore.GetUnpromoted(system, op.OpConfig.SystemEntityTypeName, op.Checkpoint);
    bags = staged.Select(se => new PromotionBag(se)).ToList();
  }
  
  public void DeserialisePendingStagedEntities() {
    if (IsEmpty()) return;
    bags.ForEach(bag => bag.SystemEntity = (ISystemEntity) Json.Deserialize(bag.StagedEntity.Data, op.OpConfig.SystemEntityType));
  }
  
  public async Task LoadExistingCoreEntities() {
    if (IsEmpty()) return;
    var sysids = bags.Select(bag => bag.SystemEntity.SystemId).ToList();
    var maps = await ctl.GetMapsFromSystemIds(system, corename, sysids);
    var coreids = maps.Select(m => m.CoreId).ToList();
    var coreents = await core.GetExistingEntities(corename, coreids);
    bags.ForEach(bag => {
      bag.Map = maps.SingleOrDefault(m => m.SystemId == bag.SystemEntity.SystemId);
      bag.PreExistingCoreEntityAndMeta = bag.Map?.CoreId is null ? null : coreents.Single(e => e.Meta.CoreId == bag.Map.CoreId);
      bag.PreExistingCoreEntityChecksum = bag.PreExistingCoreEntityAndMeta is null ? null : CoreChecksum(bag.PreExistingCoreEntityAndMeta.CoreEntity);  
    });
  }
  
  public async Task ApplyChangesToCoreEntities() {
    if (IsEmpty()) return;
    var list = await CallEvaluator();
    list.ForEach(result => {
      var bag = bags.Single(b => b.SystemEntity.SystemId == result.SystemEntity.SystemId);
      if (result is EntityToPromote topromote) bag.MarkPromote(system, topromote.CoreEntityAndMeta, op.FuncConfig.ChecksumAlgorithm); 
      else if (result is EntityToIgnore toignore) bag.MarkIgnore(toignore.IgnoreReason);
      else throw new NotSupportedException(result.GetType().Name);
    });

    async Task<List<EntityEvaluationResult>> CallEvaluator() {
      try { return await op.OpConfig.BuildCoreEntities(op, bags.Select(bag => new EntityForPromotionEvaluation(bag.SystemEntity, bag.PreExistingCoreEntityAndMeta)).ToList()); }
      catch (Exception e) {
        if (op.FuncConfig.ThrowExceptions) throw;
        error = e;
        return [];
      }
    }
  }
  
  public void IgnoreUpdatesToSameEntityInBatch() {
    if (IsEmpty()) return;
    var added = new Dictionary<SystemEntityId, bool>();
    bags = bags.OrderByDescending(bag => bag.SystemEntity.LastUpdatedDate).ToList(); 
    bags.ForEach(bag => {
      if (bag.IsIgnore) return;
      if (!added.TryAdd(bag.SystemEntity.SystemId, true)) bag.MarkIgnore(new("already added in batch, taking most recently updated"));
    });
  }
    
  public async Task IdentifyBouncedBackAndSetCorrectId() {
    if (IsEmpty()) return;
    var topromote = ToPromote();
    var bouncebacks = await GetBounceBacks();
    if (!bouncebacks.Any()) return;
    
    var toupdate = bouncebacks.Select(sysid => {
      var idx = topromote.FindIndex(bag => bag.SystemEntity.SystemId == sysid);
      var bag = topromote[idx];
      var props = (
          OriginalEntityAndMeta: bag.PreExistingCoreEntityAndMeta ?? throw new Exception(),
          OriginalEntityChecksum: bag.PreExistingCoreEntityChecksum,
          ToPromoteIdx: idx,
          ToPromoteEntityAndMeta: bag.UpdatedCoreEntityAndMeta ?? throw new Exception(),
          PreChangeChecksumSubset: bag.CoreEntityAndMeta.CoreEntity.GetChecksumSubset(),
          PreChangeChecksum: CoreChecksum(bag.CoreEntityAndMeta.CoreEntity),
          PostChangeChecksumSubset: new object(),
          PostChangeChecksum: new CoreEntityChecksum("*"));
      
      return props;
    }).ToList();
    
    var updated = toupdate.Select(props => {
      // If the entity is bouncing back, it will have a new Core and SystemId (as it would have been created in the second system).
      //    We need to correct the process here and make it point to the original Core/System Id as we do not want
      //    multiple core records for the same entity.
      topromote[props.ToPromoteIdx].CorrectBounceBackIds(props.OriginalEntityAndMeta);
      props.PostChangeChecksumSubset = props.ToPromoteEntityAndMeta.CoreEntity.GetChecksumSubset();
      props.PostChangeChecksum = CoreChecksum(props.ToPromoteEntityAndMeta.CoreEntity);
      return props;
    }).ToList();
    
    var msgs = updated.Select(e => $"[{e.ToPromoteEntityAndMeta.CoreEntity.DisplayName}({e.ToPromoteEntityAndMeta.Meta.CoreId})] -> OriginalCoreId[{e.OriginalEntityAndMeta.CoreEntity.CoreId}] Meaningful[{e.OriginalEntityChecksum != e.PostChangeChecksum}]:" +
          $"\n\tOriginal Checksum[{e.OriginalEntityAndMeta.CoreEntity.GetChecksumSubset()}({e.OriginalEntityChecksum})]" +
          $"\n\tNew Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]");
    Log.Information("PromoteOperationRunner: identified bounce-backs({@ChangesCount}) [{@System}/{@CoreEntityTypeName}]" + String.Join("\n", msgs), bouncebacks.Count, system, corename);
    
    var errs = updated.Where(e => e.PreChangeChecksum != e.PostChangeChecksum)
        .Select(e => $"\n[{e.ToPromoteEntityAndMeta.CoreEntity.DisplayName}({e.ToPromoteEntityAndMeta.Meta.CoreId})]:" +
          $"\n\tPrechange Checksum[{e.PreChangeChecksumSubset}({e.PreChangeChecksum})]" +
          $"\n\tPostchange Checksum[{e.PostChangeChecksumSubset}({e.PostChangeChecksum})]")
        .ToList();
    if (errs.Any())
      throw new Exception($"Bounce-back identified and after correcting ids we got a different checksum.  " +
            $"\nThe GetChecksumSubset() method of ICoreEntity should not include Ids, updated dates or any other non-meaninful data." +
            $"\nHaving the Id as part of the ChecksumSubset will mean that we will get infinite bounce backs between two systems." + errs);
    
   
    async Task<List<SystemEntityId>> GetBounceBacks() {
      return (await ctl.GetMapsFromSystemIds(
          system, corename, topromote.Select(bag => bag.CoreEntityAndMeta.Meta.OriginalSystemId).ToList()))
        .Select(m => m.SystemId).ToList();
    }
  }
    
  public void IgnoreEntitiesBouncingBack() {
    if (IsEmpty()) return;
    bags
        .Where(bag => !bag.IsIgnore && bag.CoreEntityAndMeta.Meta.OriginalSystem != system)
        .ForEach(bag => bag.MarkIgnore(new("update is a bounce-back and entity is not bi-directional")));
  }

  public void IgnoreNonMeaninfulChanges() {
    if (IsEmpty()) return;
    var topromote = ToPromote();
    var toignore = topromote.Where(bag => {
      if (bag.PreExistingCoreEntityChecksum is null) return false;
      return !String.IsNullOrWhiteSpace(bag.UpdatedCoreEntityAndMeta?.Meta.CoreEntityChecksum ?? throw new Exception()) && bag.UpdatedCoreEntityAndMeta?.Meta.CoreEntityChecksum == bag.PreExistingCoreEntityChecksum;
    }).ToList();
    toignore.ForEach(bag => bag.MarkIgnore(new("no meaningful change detected on entity")));
  } 
  
  public async Task WriteEntitiesToCoreStorageAndUpdateMaps() {
    if (IsEmpty()) return;

    await Task.WhenAll([
      core.Upsert(corename, ToPromote().Select(bag => bag.UpdatedCoreEntityAndMeta ?? throw new Exception()).ToList()),
      ctl.CreateSysMap(system, corename, ToCreate().Select(bag => bag.MarkCreated(op.FuncConfig.ChecksumAlgorithm)).ToList()),
      ctl.UpdateSysMap(system, corename, ToUpdate().Select(bag => bag.MarkUpdated(op.FuncConfig.ChecksumAlgorithm)).ToList())
    ]);
  }
  
  public async Task UpdateAllStagedEntitiesWithNewState(IStagedEntityRepository stagestore) {
    if (error is not null) return;
    await stagestore.UpdateImpl(op.State.System, op.OpConfig.SystemEntityTypeName, bags.Select(bag => bag.IsIgnore ? bag.StagedEntity.Ignore(bag.IgnoreReason!) : bag.StagedEntity.Promote(start)).ToList());
  }

  public void LogPromotionSteps() {
    Log.Information($"PromotionSteps completed[{system}/{corename}] Bidi[{op.OpConfig.IsBidirectional}] IsEmpty[{IsEmpty()}] Total[{bags.Count}] ToIgnore[{ToIgnore().Count}] ToPromote[{ToPromote().Count}] ToUpdate[{ToUpdate().Count}] ToCreate[{ToCreate().Count}]");
  }
  
  public PromoteOperationResult GetResults() {
    if (error is not null) return new ErrorPromoteOperationResult(EOperationAbortVote.Abort, error);
    
    var topromote = ToPromote().Select(bag => (bag.StagedEntity, Sys: bag.SystemEntity, Entity: bag.CoreEntityAndMeta.CoreEntity)).ToList();
    var toignore = ToIgnore().Select(bag => (bag.StagedEntity, bag.IgnoreReason!)).ToList();
    return new SuccessPromoteOperationResult(topromote, toignore);
  }
  
  public bool IsEmpty() => error is not null || bags.All(bag => bag.IsIgnore);
  public List<PromotionBag> ToPromote() => bags.Where(bag => !bag.IsIgnore).ToList();
  public List<PromotionBag> ToCreate() => ToPromote().Where(bag => bag.Map is null).ToList();
  public List<PromotionBag> ToUpdate() => ToPromote().Where(bag => bag.Map is not null).ToList();
  public List<PromotionBag> ToIgnore() => bags.Where(bag => bag.IsIgnore).ToList();
  public CoreEntityChecksum CoreChecksum(ICoreEntity coreent) => op.FuncConfig.ChecksumAlgorithm.Checksum(coreent);
}