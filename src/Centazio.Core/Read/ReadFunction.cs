using Centazio.Core.Ctl;
using Centazio.Core.Stage;

namespace Centazio.Core.Read;

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