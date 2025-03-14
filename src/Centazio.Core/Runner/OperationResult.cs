using System.Text.Json.Serialization;

namespace Centazio.Core.Runner;

public record OpResultAndObject(ObjectName Object, OperationResult Result);

public abstract record OperationResult(
    EOperationResult Result,
    string Message,
    int ChangedCount,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    DateTime? NextCheckpoint = null,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}

internal sealed record ErrorOperationResult(int ChangedCount, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(EOperationResult.Error, $"ErrorOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}] ChangedCount[{ChangedCount}]", ChangedCount, AbortVote, null, Exception);