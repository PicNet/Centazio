using System.Diagnostics;
using Centazio.Core.Ctl;
using Centazio.Core.Stage;

namespace Centazio.Core.Read;

public abstract class ReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl) : 
    AbstractFunction<ReadOperationConfig>(system, LifecycleStage.Defaults.Read, ctl) {
  
  protected ReadOperationResult CreateResult(List<RawJsonData> results, EOperationAbortVote abort = EOperationAbortVote.Continue) {
    if (!results.Any()) return ReadOperationResult.EmptyResult(abort);
    var (correlated, checkpoint) = (results.Select(GetCorrelationIdIfRequired).ToList(), GetMaxLastUpdatedDateOrLastFuncStartTimeAsUtc());
    return ReadOperationResult.Create(correlated, checkpoint, abort);
    
    // todo GT: get propert correlation id here instead of `new(nameof(CorrelationId))`
    RawJsonDataWithCorrelationId GetCorrelationIdIfRequired(RawJsonData raw) => 
        raw as RawJsonDataWithCorrelationId ?? new RawJsonDataWithCorrelationId(raw.Json, new(nameof(CorrelationId)), raw.Id, raw.LastUpdatedUtc);

    DateTime GetMaxLastUpdatedDateOrLastFuncStartTimeAsUtc() => results
        .Where(r => r.LastUpdatedUtc is not null)
        .DefaultIfEmpty(new RawJsonData(String.Empty, String.Empty, FunctionStartTime))
        .Max(r => r.LastUpdatedUtc ?? throw new UnreachableException());
  }

  public override async Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
    var res = await op.OpConfig.GetUpdatesAfterCheckpoint(op);
    if (res is not ListReadOperationResult lr) { return res; }
    
    var staged = await stager.StageItems(op.State.System, op.OpConfig.SystemEntityTypeName, lr.PayloadList);
    // IEntityStager.StageItems can ignore previously staged items, so adjust the count here to avoid redundant
    //    function-to-function triggers/notifications
    var uniques = staged.Select(s => lr.PayloadList.Single(r => r.Json == s.Data.Value)).ToList();
    return ReadOperationResult.Create(uniques, lr.SpecificNextCheckpoint, lr.AbortVote);
  }
}

