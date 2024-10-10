using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;
public class WriteOperationRunner<C>(ICoreToSystemMapStore entitymap, ICoreStorageGetter core) : 
    IOperationRunner<C, WriteOperationResult> where C : WriteOperationConfig {
  
  public async Task<WriteOperationResult> RunOperation(OperationStateAndConfig<C> op) {
    var pending = await core.Get(op.State.System, op.State.Object.ToCoreEntityType, op.Checkpoint);
    var (tocreate, toupdate) = await entitymap.GetNewAndExistingMappingsFromCores(op.State.System, pending);
    if (!tocreate.Any() && !toupdate.Any()) return new SuccessWriteOperationResult([], []);
    
    var (syscreates, sysupdates) = await op.OpConfig.TargetSysWriter.CovertCoreEntitiesToSystemEntitties(op.OpConfig, tocreate, toupdate);
    
    var meaningful = RemoveNonMeaninfulChanges(op, sysupdates); 
    Log.Information($"WriteOperationRunner [{op.State.System.Value}/{op.State.Object.Value}] Checkpoint[{op.Checkpoint:o}] Pending[{pending.Count}] ToCreate[{syscreates.Count}] ToUpdate[{sysupdates.Count}] Meaningful[{meaningful.Count}]");
    if (!meaningful.Any() && !syscreates.Any()) return new SuccessWriteOperationResult([], []);
    
    var results = await op.OpConfig.TargetSysWriter.WriteEntitiesToTargetSystem(op.OpConfig, syscreates, sysupdates);
    
    if (results.Result == EOperationResult.Error) {
      Log.Warning("error occurred calling `WriteEntitiesToTargetSystem` {@Results}", results);
      return results;  
    }
    
    await entitymap.Create(op.State.System, op.State.Object.ToCoreEntityType, results.EntitiesCreated.Select(e => e.Map).ToList());
    await entitymap.Update(op.State.System, op.State.Object.ToCoreEntityType, results.EntitiesUpdated.Select(e => e.Map).ToList());
    return results;
  }

  private List<CoreSystemAndPendingUpdateMap> RemoveNonMeaninfulChanges(OperationStateAndConfig<C> op, List<CoreSystemAndPendingUpdateMap> toupdate) {
    return toupdate
        .Where(IsMeaningful)
        .ToList();
  
    bool IsMeaningful(CoreSystemAndPendingUpdateMap cpum) {
      var oldcs = cpum.Map.SystemEntityChecksum;
      var newcs = op.FuncConfig.ChecksumAlgorithm.Checksum(cpum.SysEnt);
      var meaningful = String.IsNullOrWhiteSpace(oldcs) || String.IsNullOrWhiteSpace(newcs) || oldcs != newcs;
      Log.Debug($"IsMeaningful[{meaningful}] CoreEntity[{cpum.Map.CoreEntityType}] Name(Id)[{cpum.Core.DisplayName}({cpum.Core.CoreId})] Old Checksum[{oldcs}] New Checksum[{newcs}]");
      return meaningful;
    }
  }

  public WriteOperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult(EOperationAbortVote.Abort, ex);
}
