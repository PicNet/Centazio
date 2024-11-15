﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Ctl;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;
public class WriteOperationRunner<C>(ICtlRepository ctl, ICoreStorage core) : IOperationRunner<C> where C : WriteOperationConfig {
  
  public async Task<OperationResult> RunOperation(OperationStateAndConfig<C> op) {
    var coretype = op.State.Object.ToCoreEntityTypeName;
    var pending = await core.GetEntitiesToWrite(op.State.System, coretype, op.Checkpoint);
    var (tocreate, toupdate) = await ctl.GetNewAndExistingMapsFromCores(op.State.System, coretype, pending.Select(t => t.CoreEntity).ToList());
    if (!tocreate.Any() && !toupdate.Any()) return new SuccessWriteOperationResult([], []);
    
    ValidateToCreateAndUpdates();
    Log.Debug($"WriteOperationRunner calling CovertCoreEntitiesToSystemEntitties[{op.State.System}/{op.State.Object}] ToCreate[{tocreate.Count}] ToUpdate[{toupdate.Count}]");
    var (syscreates, sysupdates) = await op.OpConfig.CovertCoreEntitiesToSystemEntities(new (op.OpConfig, tocreate, toupdate));
    
    var meaningful = RemoveNonMeaninfulChanges(op, sysupdates); 
    Log.Information($"WriteOperationRunner [{op.State.System}/{op.State.Object}] Checkpoint[{op.Checkpoint:o}] Pending[{pending.Count}] ToCreate[{syscreates.Count}] ToUpdate[{sysupdates.Count}] Meaningful[{meaningful.Count}]");
    if (!meaningful.Any() && !syscreates.Any()) return new SuccessWriteOperationResult([], []);
    
    Log.Debug($"WriteOperationRunner calling WriteEntitiesToTargetSystem[{op.State.System}/{op.State.Object}] Created[{syscreates.Count}] Updated[{sysupdates.Count}]");
    var results = await op.OpConfig.WriteEntitiesToTargetSystem(new (op.OpConfig, syscreates, sysupdates));
    
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

  private List<CoreSystemAndPendingUpdateMap> RemoveNonMeaninfulChanges(OperationStateAndConfig<C> op, List<CoreSystemAndPendingUpdateMap> toupdate) {
    return toupdate
        .Where(IsMeaningful)
        .ToList();
  
    bool IsMeaningful(CoreSystemAndPendingUpdateMap cpum) {
      var oldcs = cpum.Map.SystemEntityChecksum;
      var newcs = op.FuncConfig.ChecksumAlgorithm.Checksum(cpum.SystemEntity);
      var meaningful = String.IsNullOrWhiteSpace(oldcs) || String.IsNullOrWhiteSpace(newcs) || oldcs != newcs;
      Log.Debug($"IsMeaningful[{meaningful}] CoreEntity[{cpum.Map.CoreEntityTypeName}] Name(Id)[{cpum.CoreEntity.DisplayName}({cpum.CoreEntity.CoreId})] Old Checksum[{oldcs}] New Checksum[{newcs}]");
      return meaningful;
    }
  }

  public OperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult(EOperationAbortVote.Abort, ex);
}
