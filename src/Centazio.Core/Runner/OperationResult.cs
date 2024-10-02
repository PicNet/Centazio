using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Runner;

public abstract record OperationResult(
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}