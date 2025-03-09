using System.Text.Json.Serialization;

namespace Centazio.Core.Runner;

public abstract record OperationResult(
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    DateTime? NextCheckpoint = null,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}

internal sealed record ErrorOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(EOperationResult.Error, $"ErrorOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]", AbortVote, null, Exception);