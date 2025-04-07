namespace Centazio.Core.Read;

public abstract record ReadOperationResult(
    EOperationResult Result,
    string Message, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    DateTime? NextCheckpoint = null,
    Exception? Exception = null) : OperationResult(Result, Message, ResultLength, AbortVote, NextCheckpoint, Exception), ILoggable {
  
  public static ReadOperationResult EmptyResult(EOperationAbortVote abort = EOperationAbortVote.Continue) => new EmptyReadOperationResult(abort);
  public static ReadOperationResult Create(List<string> lst, DateTime nextcheckpoint, EOperationAbortVote abort = EOperationAbortVote.Continue) => 
      !lst.Any() ? throw new Exception("Empty results should return ReadOperationResult.EmptyResult()") : new ListReadOperationResult(lst, nextcheckpoint, abort);
  
  public string LoggableValue => $"{Result} -> {ResultLength} Message[{Message}]";

}

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