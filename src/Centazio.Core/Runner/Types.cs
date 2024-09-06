using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Runner;

public record FunctionConfig<T>(SystemName System, LifecycleStage Stage, ValidList<T> Operations) where T : OperationConfig;

public abstract record OperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint);
public record ReadOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<ReadOperationConfig>, Task<OperationResult>> GetObjectsToStage) : OperationConfig(Object, Cron, FirstTimeCheckpoint);
public record PromoteOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<PromoteOperationConfig>, IEnumerable<StagedEntity>, Task<PromoteOperationResult>> PromoteObjects) : OperationConfig(Object, Cron, FirstTimeCheckpoint);
public record PromoteOperationResult(OperationResult OpResult, IEnumerable<StagedEntity> Promoted, IEnumerable<(StagedEntity Entity, ValidString Reason)> Ignored);

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim(), CronFormat.IncludeSeconds); 
  }
  
  public CronExpression Value {get; }

  public static implicit operator CronExpression(ValidCron value) => value.Value;
  public static implicit operator ValidCron(string value) => new(value);
}

public record OperationStateAndConfig<T>(ObjectState State, T Settings) where T : OperationConfig {
  public DateTime Checkpoint => State.LastSuccessStart ?? Settings.FirstTimeCheckpoint;
}

public abstract record OperationResult(
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  static OperationResult() {
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(EmptyOperationResult).TypeHandle);
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SingleRecordOperationResult).TypeHandle);
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ListRecordOperationResult).TypeHandle);
  }
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
  public bool IsValid { get; } = Result != EOperationResult.Error && ResultLength > 0; 
  
  public static OperationResult Empty(EOperationAbortVote abort = EOperationAbortVote.Continue) => Create(EOperationResult.Success, String.Empty, abort);
  public static OperationResult Success(string? payload, EOperationAbortVote abort = EOperationAbortVote.Continue) => Create(EOperationResult.Success, payload, abort);
  public static OperationResult Success(IEnumerable<string>? payload, EOperationAbortVote abort = EOperationAbortVote.Continue) => Create(EOperationResult.Success, payload, abort);
  
  public static OperationResult Error(EOperationAbortVote abort = EOperationAbortVote.Continue, Exception? ex = null) => EmptyOperationResultFactory(EOperationResult.Error, "error", abort, ex);
  
  private static OperationResult Create(EOperationResult result, string? payload, EOperationAbortVote abort = EOperationAbortVote.Continue) {
    if (String.IsNullOrWhiteSpace(payload)) return EmptyOperationResultFactory(result, "empty payload", abort, null);
    return SingleRecordOperationResultFactory(result, "single item payload", payload, abort) ;
  }
  
  private static OperationResult Create(EOperationResult result, IEnumerable<string>? payload, EOperationAbortVote abort = EOperationAbortVote.Continue) {
    var lst = payload?.ToList();
    if (lst == null || !lst.Any()) return EmptyOperationResultFactory(result, "empty payload", abort, null);
    if (lst.Any(String.IsNullOrWhiteSpace)) throw new ArgumentNullException($"payload has null or empty values"); 
    return ListRecordOperationResultFactory(result, "list payload", lst, abort) ;
  }
  
  // note: these 'Factory' hacks allow us to have private constructors in these records whilst still allowing the types themselves
  //    to be internal (used by ReadOperationRunner)
  private static Func<EOperationResult, string, EOperationAbortVote, Exception?, EmptyOperationResult> EmptyOperationResultFactory = null!;
  internal record EmptyOperationResult : OperationResult {
    static EmptyOperationResult() { EmptyOperationResultFactory = (result, message, abort, ex) => new EmptyOperationResult(result, message, abort, ex); }
    
    private EmptyOperationResult(EOperationResult result, string message, EOperationAbortVote abort, Exception? ex) : base(result, message, EResultType.Empty, 0, abort, ex) {}
  }

  private static Func<EOperationResult, string, string, EOperationAbortVote, SingleRecordOperationResult> SingleRecordOperationResultFactory = null!;
  internal record SingleRecordOperationResult : OperationResult {
    static SingleRecordOperationResult() { SingleRecordOperationResultFactory = (result, message, payload, abort) => new SingleRecordOperationResult(result, message, payload, abort); }
    
    internal ValidString Payload { get; } 
    
    private SingleRecordOperationResult(EOperationResult result, string message, ValidString payload, EOperationAbortVote abort = EOperationAbortVote.Continue) 
      : base(result, message, EResultType.Single, payload.Value.Length, abort) {
      Payload = payload;
    }
  }

  private static Func<EOperationResult, string, ValidList<string>, EOperationAbortVote, ListRecordOperationResult> ListRecordOperationResultFactory = null!;
  internal record ListRecordOperationResult : OperationResult {
    static ListRecordOperationResult() { ListRecordOperationResultFactory = (result, message, payload, abort) => new ListRecordOperationResult(result, message, payload, abort); }
    
    internal ValidList<string> PayloadList { get; } 
    
    private ListRecordOperationResult(EOperationResult result, string message, ValidList<string> payload, EOperationAbortVote abort = EOperationAbortVote.Continue)
      : base(result, message, EResultType.List, payload.Value.Count, abort) {
      PayloadList = payload;
    }
  }
}


