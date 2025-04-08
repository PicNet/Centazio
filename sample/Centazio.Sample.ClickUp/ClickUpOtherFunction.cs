using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Sample.ClickUp;

public class ClickUpOtherFunction(ICtlRepository ctl) : AbstractFunction<EmptyFunctionOperationConfig>(ClickUpConstants.ClickUpSystemName, new LifecycleStage("Other"), ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new EmptyFunctionOperationConfig(nameof(ClickUpOtherFunction))
  ]);

  public override Task<OperationResult> RunOperation(OperationStateAndConfig<EmptyFunctionOperationConfig> op) => 
      Task.FromResult<OperationResult>(new EmptyFunctionOperationResult());
}

public record EmptyFunctionOperationConfig(string Name) : OperationConfig(new CoreEntityTypeName("Object"), [], CronExpressionsHelper.EveryXSeconds(20)) {
  public override bool ShouldRunBasedOnTriggers(List<ObjectChangeTrigger> triggeredby) => true;
}

public record EmptyFunctionOperationResult() : OperationResult(EOperationResult.Success, nameof(Message), 0);