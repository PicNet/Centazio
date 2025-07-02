using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.TestFunctions;

public record TestSettings : CentazioSettings {
  public string? NewProperty { get; init; }
  
  protected TestSettings(CentazioSettings centazio) : base (centazio) {}
}

public class TestFunctionIntegration(params List<string> environments) : IntegrationBase<TestSettings, CentazioSecrets>(environments) {

  public override Task Initialise(ServiceProvider prov) => Task.CompletedTask;
  protected override void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar) { }

}

public class EmptyFunction(ICtlRepository ctl) : AbstractFunction<EmptyFunctionOperationConfig>(Constants.System, Constants.Stage, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new EmptyFunctionOperationConfig(nameof(EmptyFunctionOperationConfig)) 
  ]);

  public override Task<OperationResult> RunOperation(OperationStateAndConfig<EmptyFunctionOperationConfig> op) => Task.FromResult<OperationResult>(new EmptyFunctionOperationResult());

}

public record EmptyFunctionOperationConfig(string Name) : OperationConfig(Constants.Object, [], CronExpressionsHelper.EveryXSeconds(20)) {
  public override bool ShouldRunBasedOnTriggers(List<ObjectChangeTrigger> triggeredby) => true;
}

public record EmptyFunctionOperationResult() : OperationResult(EOperationResult.Success, nameof(Message), 0);

public static class Constants {
  internal static readonly LifecycleStage Stage = new ("TestFunctions.Stage");
  internal static readonly SystemName System = new ("TestFunctions.EmptyFunction");
  internal static readonly SystemEntityTypeName Object = new ("EmptyFunction.SystemEntity");
}