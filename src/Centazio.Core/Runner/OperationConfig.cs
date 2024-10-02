using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Runner;

public abstract record OperationConfig<O>(
    O Object, 
    ValidCron Cron) where O : ObjectName {
  public DateTime? FirstTimeCheckpoint { get; init; }
}

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim(), CronFormat.IncludeSeconds); 
  }
  
  public CronExpression Value {get; }

  public static implicit operator CronExpression(ValidCron value) => value.Value;
  public static implicit operator ValidCron(string value) => new(value);
}

public record OperationStateAndConfig<C, O>(
    ObjectState<O> State,
    IFunctionConfig FuncConfig,
    C OpConfig, 
    DateTime Checkpoint) 
    where C : OperationConfig<O> 
    where O : ObjectName;