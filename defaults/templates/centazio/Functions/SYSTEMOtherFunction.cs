using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace {{ it.Namespace }};

public class {{ it.SystemName }}OtherFunction(ICtlRepository ctl) : AbstractFunction<EmptyFunctionOperationConfig>({{ it.SystemName }}Constants.{{ it.SystemName }}SystemName, new LifecycleStage("Other"), ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new EmptyFunctionOperationConfig(nameof({{ it.SystemName }}OtherFunction))
  ]);

  public override Task<OperationResult> RunOperation(OperationStateAndConfig<EmptyFunctionOperationConfig> op) => 
      Task.FromResult<OperationResult>(new EmptyFunctionOperationResult());
}

public record EmptyFunctionOperationConfig(string Name) : OperationConfig(new CoreEntityTypeName("Object"), [], CronExpressionsHelper.EveryXSeconds(20));

public record EmptyFunctionOperationResult() : OperationResult(EOperationResult.Success, nameof(Message), 0);