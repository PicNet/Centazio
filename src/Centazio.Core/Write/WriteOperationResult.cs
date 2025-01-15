using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult(
    List<Map.Created> EntitiesCreated,
    List<Map.Updated> EntitiesUpdated,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, null, Exception), ILoggable {

  public int TotalChanges => EntitiesCreated.Count + EntitiesUpdated.Count;
  
  public string LoggableValue => $"EntitiesCreated[{EntitiesCreated.Count}] EntitiesUpdated[{EntitiesUpdated.Count}] Result[{Result}] Message[{Message}]";
}

public record SuccessWriteOperationResult(
    List<Map.Created> EntitiesCreated,
    List<Map.Updated> EntitiesUpdated,
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