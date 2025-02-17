using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.TestFunctions;

public class TestFunctionIntegration(string environment) : IntegrationBase<CentazioSettings, CentazioSecrets>(environment) {

  public override Task Initialise(ServiceProvider prov) => Task.CompletedTask;
  protected override void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar) { }

}

public class EmptyFunction(ICtlRepository ctl) : AbstractFunction<EmptyFunctionOperationConfig>(Constants.System, Constants.Stage, new EmptyFunctionOperationRunner(), ctl) {

  protected override FunctionConfig<EmptyFunctionOperationConfig> GetFunctionConfiguration() => new([
    new EmptyFunctionOperationConfig(nameof(EmptyFunctionOperationConfig)) 
  ]);

}

public class EmptyFunctionOperationRunner : IOperationRunner<EmptyFunctionOperationConfig> {
  public Task<OperationResult> RunOperation(OperationStateAndConfig<EmptyFunctionOperationConfig> op) {
    Log.Information($"EmptyFunctionOperationRunner#RunOperation[{op.OpConfig.Name}]");
    return Task.FromResult<OperationResult>(new EmptyFunctionOperationResult());
  }
}

public record EmptyFunctionOperationConfig(string Name) : OperationConfig(Constants.Object, CronExpressionsHelper.EveryXSeconds(20));

public record EmptyFunctionOperationResult() : OperationResult(EOperationResult.Success, nameof(Message));

public static class Constants {
  internal static readonly LifecycleStage Stage = new ("TestFunctions.Stage");
  internal static readonly SystemName System = new ("TestFunctions.EmptyFunction");
  internal static readonly SystemEntityTypeName Object = new ("EmptyFunction.SystemEntity");
}