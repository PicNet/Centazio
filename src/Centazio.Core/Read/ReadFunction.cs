using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Stage;

namespace Centazio.Core.Read;

public abstract class ReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl) : 
    AbstractFunction<ReadOperationConfig>(system, LifecycleStage.Defaults.Read, ctl) {
  
  protected ReadOperationResult CreateResult(List<RawJsonData> results, EOperationAbortVote abort = EOperationAbortVote.Continue) {
    if (!results.Any()) return ReadOperationResult.EmptyResult(abort);
    return ReadOperationResult.Create(results, GetMaxLastUpdatedDateOrFuncStartTime(), abort);
    
    DateTime GetMaxLastUpdatedDateOrFuncStartTime() => results
        .Where(r => r.LastUpdatedUtc is not null)
        .DefaultIfEmpty(new RawJsonData(String.Empty, String.Empty, FunctionStartTime))
        .Max(r => r.LastUpdatedUtc ?? throw new UnreachableException());
  }

  public override async Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.OpConfig.GetUpdatesAfterCheckpoint(op);
    if (res is not ListReadOperationResult lr) { return res; }
    
    var type = op.OpConfig.SystemEntityTypeName;
    var staged = await stager.StageItems(System, type, AddCorrelations(type, lr.PayloadList));
    // IEntityStager.StageItems can ignore previously staged items, so adjust the count here to avoid redundant
    //    function-to-function triggers/notifications
    var uniques = staged.Select(s => lr.PayloadList.Single(r => r.Json == s.Data.Value)).ToList();
    return ReadOperationResult.Create(uniques, lr.SpecificNextCheckpoint, lr.AbortVote);
  }

  private List<RawJsonDataWithCorrelationId> AddCorrelations(SystemEntityTypeName type, List<RawJsonData> data) {
    return data.Select(r => r.AddCorrelation(new("todo GT: implement"))).ToList();
  }

}

