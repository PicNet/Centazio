﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;
public class WriteOperationRunner<C>(ICoreToSystemMapStore entitymap, ICoreStorageGetter core) : 
    IOperationRunner<C, WriteOperationResult> where C : WriteOperationConfig {
  
  public async Task<WriteOperationResult> RunOperation(OperationStateAndConfig<C> op) {
    var pending = await core.Get(op.State.Object.ToCoreEntityType, op.Checkpoint, op.State.System);
    var maps = await entitymap.GetNewAndExistingMappingsFromCores(pending, op.State.System);
    var meaningful = await RemoveNonMeaninfulChanges(op, maps); 
    Log.Information($"WriteOperationRunner [{op.State.System.Value}/{op.State.Object.Value}] Checkpoint[{op.Checkpoint:o}] Pending[{pending.Count}] Created[{maps.Created.Count}] Updated[{maps.Updated.Count}] Meaningful Updates[{meaningful.Updated.Count}]");
    if (!meaningful.Updated.Any() && !meaningful.Created.Any()) return new SuccessWriteOperationResult([], []);
    var results = await op.OpConfig.TargetSysWriter.WriteEntitiesToTargetSystem(op.OpConfig, meaningful.Created, meaningful.Updated);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning("error occurred calling `WriteEntitiesToTargetSystem` {@Results}", results);
      return results;  
    }
    
    await entitymap.Create(op.State.Object.ToCoreEntityType, op.State.System, results.EntitiesCreated.Select(e => e.Map).ToList());
    await entitymap.Update(op.State.Object.ToCoreEntityType, op.State.System, results.EntitiesUpdated.Select(e => e.Map).ToList());
    return results;
  }

  private async Task<WriteEntitiesToTargetSystem> RemoveNonMeaninfulChanges(OperationStateAndConfig<C> op, GetForCoresResult maps) {
    var updated = new List<CoreSystemMap>();
    foreach (var cpum in maps.Updated) {
      var check = await IsMeaningful(cpum);
      if (check.IsMeaningful) updated.Add(check.Details);
    } 
    return new WriteEntitiesToTargetSystem(maps.Created, updated);

    async Task<(bool IsMeaningful, CoreSystemMap Details)> IsMeaningful(CoreAndPendingUpdateMap cpum) {
      // todo: change this, it should be done for whole batch not entity by entity
      var external = await op.OpConfig.TargetSysWriter.CovertCoreEntityToSystemEntity(op.OpConfig, cpum.Core, cpum.Map);
      var existing = cpum.Map.Checksum;
      var changed = op.FuncConfig.ChecksumAlgorithm.Checksum(external);
      var meaningful = String.IsNullOrWhiteSpace(existing) || String.IsNullOrWhiteSpace(changed) || existing != changed;
      Log.Debug($"IsMeaningful[{meaningful}] CoreEntity[{cpum.Map.CoreEntity}] Name(Id)[{cpum.Core.DisplayName}({cpum.Core.Id})] Old Checksum[{existing}] New Checksum[{changed}]");
      return (meaningful, cpum.SetSystemEntity(external, changed));
    }
  }

  public WriteOperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult(EOperationAbortVote.Abort, ex);
}
