using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

internal class PromoteOperationRunner(IStagedEntityStore staged) 
    : IOperationRunner<PromoteOperationConfig, PromoteOperationResult> {
  
  public async Task<PromoteOperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<PromoteOperationConfig> op) {
    var pending = await staged.Get(op.Checkpoint, op.State.System, op.State.Object);
    var results = await op.Settings.EvaluateEntitiesToPromote(op, pending);
    
    var promote = results.ToPromote.ToList();
    var ignore = results.ToIgnore.ToList();
    
    if (promote.Any()) WriteEntitiesToCoreStorage(op, promote.Select(p => p.Core).ToList());
    
    await staged.Update(
        promote.Select(e => e.Staged with { DatePromoted = funcstart }).Concat(
            ignore.Select(e => e.Entity with { Ignore = e.Reason })));
    
    return results; 
  }

  private static void WriteEntitiesToCoreStorage(OperationStateAndConfig<PromoteOperationConfig> op, List<ICoreEntity> entities) {
    // ignore multiple of the same entity staged, just promote the latest update
    var nodups = entities
        .GroupBy(c => c.SourceId)
        .Select(g => g.OrderByDescending(c => c.LastSourceSystemUpdate).First())
        .ToList();
    // todo: ignore entities that are already in core storage but were created (and hence owned) by other system
    //  I dont understand this, was this a hack?
    /*
      var externalids = db.TargetSystemEntity.
          Where(tse => tse.CoreEntity == typeof(C).Name && tse.TargetSystem == state.System && tse.TargetPk != null && ids.Contains(tse.TargetPk)).
          Select(tse => tse.TargetPk!).
          ToList();
       */
    if (!nodups.Any()) return;
    
    op.Settings.PromoteEntities(op, nodups);
  }

  public PromoteOperationResult BuildErrorResult(OperationStateAndConfig<PromoteOperationConfig> op, Exception ex) => new ErrorPromoteOperationResult(ex.Message, EOperationAbortVote.Abort, ex);

}