using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract record ReadOperationResult(
    EOperationResult Result,
    string Message, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    DateTime? NextCheckpoint = null,
    Exception? Exception = null) : OperationResult(Result, Message, AbortVote, NextCheckpoint, Exception), ILoggable {
  
  public static ReadOperationResult EmptyResult() => new EmptyReadOperationResult();
  public static ReadOperationResult Create(List<string> lst, DateTime nextcheckpoint) => !lst.Any() ? throw new Exception("Empty results should return ReadOperationResult.EmptyResult()") : new ListReadOperationResult(lst, nextcheckpoint);
  
  public string LoggableValue => $"{Result} -> {ResultLength} Message[{Message}]";

}

internal record ErrorReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : ReadOperationResult(EOperationResult.Error, $"ErrorReadOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]", 0, AbortVote, null, Exception);

internal record EmptyReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(EOperationResult.Success, "EmptyReadOperationResult", 0, AbortVote);

internal record ListReadOperationResult(List<string> PayloadList, DateTime SpecificNextCheckpoint, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success,
        $"ListReadOperationResult[{PayloadList.Count}]", 
        PayloadList.Any() ? PayloadList.Count : throw new ArgumentNullException(), 
        AbortVote, SpecificNextCheckpoint) {
  public List<string> PayloadList { get; } = PayloadList.Any() && !PayloadList.Any(String.IsNullOrWhiteSpace) 
      ? PayloadList : throw new ArgumentNullException(nameof(PayloadList));
}