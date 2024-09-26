using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult(
    ICollection<(ICoreEntity Core, EntityIntraSysMap.Created Map)> EntitiesCreated,
    ICollection<(ICoreEntity Core, EntityIntraSysMap.Updated Map)> EntitiesUpdated,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, Exception) {

  public int TotalChanges => EntitiesCreated.Count + EntitiesUpdated.Count;

}

public record SuccessWriteOperationResult(
    ICollection<(ICoreEntity Core, EntityIntraSysMap.Created Map)> EntitiesCreated,
    ICollection<(ICoreEntity Core, EntityIntraSysMap.Updated Map)> EntitiesUpdated,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : WriteOperationResult(EntitiesCreated,
        EntitiesUpdated,
        EOperationResult.Success,
        $"SuccessWriteOperationResult Created[{EntitiesCreated.Count}] Updated[{EntitiesUpdated.Count}]",
        AbortVote);

public record ErrorWriteOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null)
    : WriteOperationResult(Array.Empty<(ICoreEntity Core, EntityIntraSysMap.Created Map)>(),
        Array.Empty<(ICoreEntity Core, EntityIntraSysMap.Updated Map)>(),
        EOperationResult.Error,
        $"ErrorWriteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]",
        AbortVote,
        Exception);