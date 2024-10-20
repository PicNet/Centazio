using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Ctl;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Core.Write;
public class WriteOperationRunner<C>(ICtlRepository ctl, ICoreStorageGetter core) : 
    IOperationRunner<C, WriteOperationResult> where C : WriteOperationConfig {
  
  public async Task<WriteOperationResult> RunOperation(OperationStateAndConfig<C> op) {
    var coretype = op.State.Object.ToCoreEntityTypeName;
    var pending = await core.Get(op.State.System, coretype, op.Checkpoint);
    var (tocreate, toupdate) = await ctl.GetNewAndExistingMapsFromCores(op.State.System, coretype, pending);
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
    
    await ctl.CreateSysMap(op.State.System, coretype, results.EntitiesCreated);
    await ctl.UpdateSysMap(op.State.System, coretype, results.EntitiesUpdated);
    return results;
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

  public WriteOperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex) => new ErrorWriteOperationResult(EOperationAbortVote.Abort, ex);
}
