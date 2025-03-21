using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Stage;
using Centazio.Core.Write;

namespace Centazio.Core;

// todo: move these to their respective Read/Promote/Write namespaces

public abstract class ReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl) : 
    AbstractFunction<ReadOperationConfig>(system, LifecycleStage.Defaults.Read, ctl) {
  
  protected ReadOperationResult CreateResult(List<string> results, DateTime? nextcheckpointutc = null, EOperationAbortVote abort = EOperationAbortVote.Continue) => !results.Any() ? 
      ReadOperationResult.EmptyResult(abort) : 
      ReadOperationResult.Create(results, nextcheckpointutc ?? FunctionStartTime, abort);

  public override async Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.OpConfig.GetUpdatesAfterCheckpoint(op);
    if (res is not ListReadOperationResult lr) { return res; }
    
    // IEntityStager can ignore previously staged items, so adjust the count here to avoid redundant
    //    function-to-function triggers/notifications
    var staged = await stager.Stage(op.State.System, op.OpConfig.SystemEntityTypeName, lr.PayloadList);
    return CreateResult(staged.Select(s => s.Data.Value).ToList(), lr.SpecificNextCheckpoint, lr.AbortVote);
  }

}

public abstract class PromoteFunction(SystemName system, IStagedEntityRepository stage, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<PromoteOperationConfig>(system, LifecycleStage.Defaults.Promote, ctl) {

  public override async Task<OperationResult> RunOperation(OperationStateAndConfig<PromoteOperationConfig> op) {
    var steps = new PromotionSteps(core, ctl, op);
    await steps.LoadPendingStagedEntities(stage);
    await steps.LoadExistingCoreEntities();
    await steps.ApplyChangesToCoreEntities();
    steps.IgnoreUpdatesToSameEntityInBatch();
    
    if (op.OpConfig.IsBidirectional) await steps.IdentifyBouncedBackAndSetCorrectId(); 
    else steps.IgnoreEntitiesBouncingBack();
    
    steps.IgnoreNonMeaninfulChanges();
    await steps.WriteEntitiesToCoreStorageAndUpdateMaps();
    await steps.UpdateAllStagedEntitiesWithNewState(stage);
    steps.LogPromotionSteps();
    
    return steps.GetResults();

  }
}

public delegate ISystemEntity ConvertCoreToSystemEntityForWritingHandler<E>(SystemEntityId systemid, E coreent) where E : ICoreEntity;

public abstract class WriteFunction(SystemName system, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<WriteOperationConfig>(system, LifecycleStage.Defaults.Write, ctl) {

  public override async Task<OperationResult> RunOperation(OperationStateAndConfig<WriteOperationConfig> op) {
    var coretype = op.State.Object.ToCoreEntityTypeName;
    var pending = await core.GetEntitiesToWrite(op.State.System, coretype, op.Checkpoint);
    var (tocreate, toupdate) = await ctl.GetNewAndExistingMapsFromCores(op.State.System, coretype, pending.Select(t => t.CoreEntity).ToList());
    if (!tocreate.Any() && !toupdate.Any()) return new SuccessWriteOperationResult([], []);
    
    ValidateToCreateAndUpdates();
    Log.Debug($"WriteOperationRunner calling CovertCoreEntitiesToSystemEntitties[{op.State.System}/{op.State.Object}] ToCreate[{tocreate.Count}] ToUpdate[{toupdate.Count}]");
    var (syscreates, sysupdates) = await op.OpConfig.CovertCoreEntitiesToSystemEntities(op.OpConfig, tocreate, toupdate);
    
    var meaningful = RemoveNonMeaninfulChanges(op, sysupdates); 
    Log.Information($"WriteOperationRunner [{op.State.System}/{op.State.Object}] Checkpoint[{op.Checkpoint:o}] Pending[{pending.Count}] ToCreate[{syscreates.Count}] ToUpdate[{sysupdates.Count}] Meaningful[{meaningful.Count}]");
    if (!syscreates.Any() && !meaningful.Any()) return new SuccessWriteOperationResult([], []);
    
    Log.Debug($"WriteOperationRunner calling WriteEntitiesToTargetSystem[{op.State.System}/{op.State.Object}] Created[{syscreates.Count}] Updated[{sysupdates.Count}]");
    var flows = syscreates.Select(e => "Add: " + e.CoreEntity.GetShortDisplayName()).Concat(meaningful.Select(e => "Edit: " + e.CoreEntity.GetShortDisplayName())).ToList();
    DataFlowLogger.Log("Core Storage", op.State.Object, op.State.System, flows);
    var results = await op.OpConfig.WriteEntitiesToTargetSystem(op.OpConfig, syscreates, sysupdates);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning("error occurred calling `WriteEntitiesToTargetSystem` {@Results}", results);
      return results;  
    }
    
    await ctl.CreateSysMap(op.State.System, coretype, results.EntitiesCreated);
    await ctl.UpdateSysMap(op.State.System, coretype, results.EntitiesUpdated);
    return results;

    void ValidateToCreateAndUpdates() {
      var (createids, updateids) = (tocreate.Select(e => e.CoreEntity.CoreId).ToList(), toupdate.Select(e => e.CoreEntity.CoreId).ToList());
      var (createdups, updatedups) = (createids.GroupBy(id => id).Where(g => g.Count() > 1).ToList(), updateids.GroupBy(id => id).Where(g => g.Count() > 1).ToList());
      var boths = updateids.Distinct().Where(id => createids.Contains(id)).ToList();
      if (createdups.Any()) throw new Exception($"CtlRepository.GetNewAndExistingMapsFromCores returned multiple copies of entities to create [{String.Join(", ", createdups.Select(g => g.Key))}]");
      if (updatedups.Any()) throw new Exception($"CtlRepository.GetNewAndExistingMapsFromCores returned multiple copies of entities to update [{String.Join(", ", updatedups.Select(g => g.Key))}]");
      if (boths.Any()) throw new Exception($"CtlRepository.GetNewAndExistingMapsFromCores returned entities to both create and update [{String.Join(", ", boths)}]");
    }
  }

  private List<CoreSystemAndPendingUpdateMap> RemoveNonMeaninfulChanges(OperationStateAndConfig<WriteOperationConfig> op, List<CoreSystemAndPendingUpdateMap> toupdate) {
    return toupdate
        .Where(IsMeaningful)
        .ToList();
  
    bool IsMeaningful(CoreSystemAndPendingUpdateMap cpum) {
      var oldcs = cpum.Map.SystemEntityChecksum;
      var newcs = op.FuncConfig.ChecksumAlgorithm.Checksum(cpum.SystemEntity);
      var meaningful = String.IsNullOrWhiteSpace(oldcs) || String.IsNullOrWhiteSpace(newcs) || oldcs != newcs;
      Log.Debug($"IsMeaningful[{meaningful}] CoreEntity[{cpum.Map.CoreEntityTypeName}] Name(Id)[{cpum.CoreEntity.GetShortDisplayName()}] Old Checksum[{oldcs}] New Checksum[{newcs}]");
      return meaningful;
    }
  }
  
  protected CovertCoresToSystemsResult CovertCoresToSystems<E>(List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate, ConvertCoreToSystemEntityForWritingHandler<E> converter) where E : ICoreEntity {
    var tocreate2 = tocreate.Select(m => {
      var coreent = m.CoreEntity.To<E>();
      var sysent = converter(SystemEntityId.DEFAULT_VALUE, coreent);
      return m.AddSystemEntity(sysent, Config.ChecksumAlgorithm);
    }).ToList();
    var toupdate2 = toupdate.Select(m => {
      var coreent = m.CoreEntity.To<E>();
      var sysent = converter(m.Map.SystemId, coreent);
      if (m.Map.SystemEntityChecksum == Config.ChecksumAlgorithm.Checksum(sysent)) throw new Exception($"No changes found on [{typeof(E).Name}] -> [{sysent.GetType().Name}]:" + 
        $"\n\tUpdated Core Entity:[{Json.Serialize(coreent)}]" +
        $"\n\tUpdated Sys Entity[{sysent}]" +
        $"\n\tExisting Checksum:[{m.Map.SystemEntityChecksum}]" +
        $"\n\tChecksum Subset[{sysent.GetChecksumSubset()}]" +
        $"\n\tChecksum[{Config.ChecksumAlgorithm.Checksum(sysent)}]");
      
      return m.AddSystemEntity(sysent, Config.ChecksumAlgorithm);
    }).ToList();
    return new(tocreate2, toupdate2); 
  }
}
