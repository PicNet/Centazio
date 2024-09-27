using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult(
    ICollection<CoreAndCreatedMap> EntitiesCreated,
    ICollection<CoreAndUpdatedMap> EntitiesUpdated,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, Exception) {

  public int TotalChanges => EntitiesCreated.Count + EntitiesUpdated.Count;

}

public record SuccessWriteOperationResult(
    ICollection<CoreAndCreatedMap> EntitiesCreated,
    ICollection<CoreAndUpdatedMap> EntitiesUpdated,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : WriteOperationResult(EntitiesCreated,
        EntitiesUpdated,
        EOperationResult.Success,
        $"SuccessWriteOperationResult Created[{EntitiesCreated.Count}] Updated[{EntitiesUpdated.Count}]",
        AbortVote);

public record ErrorWriteOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) : WriteOperationResult(Array.Empty<CoreAndCreatedMap>(),
    Array.Empty<CoreAndUpdatedMap>(),
    EOperationResult.Error,
    $"ErrorWriteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]",
    AbortVote,
    Exception);