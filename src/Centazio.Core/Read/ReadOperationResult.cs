﻿namespace Centazio.Core.Read;

public abstract record ReadOperationResult(
    EOperationResult Result,
    string Message, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    DateTime? NextCheckpoint = null,
    Exception? Exception = null) : OperationResult(Result, Message, ResultLength, AbortVote, NextCheckpoint, Exception), ILoggable {
  
  public static ReadOperationResult EmptyResult() => new EmptyReadOperationResult();
  public static ReadOperationResult Create(List<string> lst, DateTime nextcheckpoint) => !lst.Any() ? throw new Exception("Empty results should return ReadOperationResult.EmptyResult()") : new ListReadOperationResult(lst, nextcheckpoint);
  
  public string LoggableValue => $"{Result} -> {ResultLength} Message[{Message}]";

}

internal sealed record EmptyReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(EOperationResult.Success, "EmptyReadOperationResult", 0, AbortVote);

internal sealed record ListReadOperationResult(List<string> PayloadList, DateTime SpecificNextCheckpoint, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success,
        $"ListReadOperationResult[{PayloadList.Count}]", 
        PayloadList.Any() ? PayloadList.Count : throw new ArgumentNullException(), 
        AbortVote, SpecificNextCheckpoint) {
  public List<string> PayloadList { get; } = PayloadList.Any() && !PayloadList.Any(String.IsNullOrWhiteSpace) 
      ? PayloadList : throw new ArgumentNullException(nameof(PayloadList));
}