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
    var (tocreate, toupdate) = await entitymap.GetNewAndExistingMappingsFromCores(pending, op.State.System);
    var (syscreates, sysupdates) = await op.OpConfig.TargetSysWriter.CovertCoreEntitiesToSystemEntitties(op.OpConfig, tocreate, toupdate);
    var meaningful = RemoveNonMeaninfulChanges(op, sysupdates); 
    Log.Information($"WriteOperationRunner [{op.State.System.Value}/{op.State.Object.Value}] Checkpoint[{op.Checkpoint:o}] Pending[{pending.Count}] ToCreate[{syscreates.Count}] ToUpdate[{sysupdates.Count}] Meaningful Updates[{meaningful.Count}]");
    if (!meaningful.Any() && !syscreates.Any()) return new SuccessWriteOperationResult([], []);
    var results = await op.OpConfig.TargetSysWriter.WriteEntitiesToTargetSystem(op.OpConfig, syscreates, sysupdates);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning("error occurred calling `WriteEntitiesToTargetSystem` {@Results}", results);
      return results;  
    }
    
    await entitymap.Create(op.State.Object.ToCoreEntityType, op.State.System, results.EntitiesCreated.Select(e => e.Map).ToList());
    await entitymap.Update(op.State.Object.ToCoreEntityType, op.State.System, results.EntitiesUpdated.Select(e => e.Map).ToList());
    return results;
  }

  private List<CoreSystemMap> RemoveNonMeaninfulChanges(OperationStateAndConfig<C> op, List<CoreSystemMap> toupdate) {
    return toupdate.Where(IsMeaningful).ToList();
  
    bool IsMeaningful(CoreSystemMap cpum) {
      var existing = cpum.Map.Checksum;
      var changed = op.FuncConfig.ChecksumAlgorithm.Checksum(cpum.SystemEntity);
      var meaningful = String.IsNullOrWhiteSpace(existing) || String.IsNullOrWhiteSpace(changed) || existing != changed;
      Log.Debug($"IsMeaningful[{meaningful}] CoreEntity[{cpum.Map.CoreEntity}] Name(Id)[{cpum.Core.DisplayName}({cpum.Core.Id})] Old Checksum[{existing}] New Checksum[{changed}]");
      return meaningful;
    }
  }

  public WriteOperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult(EOperationAbortVote.Abort, ex);
}
