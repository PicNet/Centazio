using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult(
    List<CoreAndCreatedMap> EntitiesCreated,
    List<CoreAndUpdatedMap> EntitiesUpdated,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, Exception), ILoggable {

  public int TotalChanges => EntitiesCreated.Count + EntitiesUpdated.Count;
  
  public object LoggableValue => $"EntitiesCreated[{EntitiesCreated.Count}] EntitiesUpdated[{EntitiesUpdated.Count}] Result[{Result}] Message[{Message}]";
}

public record SuccessWriteOperationResult(
    List<CoreAndCreatedMap> EntitiesCreated,
    List<CoreAndUpdatedMap> EntitiesUpdated,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : WriteOperationResult(EntitiesCreated,
        EntitiesUpdated,
        EOperationResult.Success,
        $"SuccessWriteOperationResult Created[{EntitiesCreated.Count}] Updated[{EntitiesUpdated.Count}]",
        AbortVote);

public record ErrorWriteOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) : WriteOperationResult(
    [],
    [],
    EOperationResult.Error,
    $"ErrorWriteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]",
    AbortVote,
    Exception);