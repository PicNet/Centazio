using Centazio.Cli.Commands.Aws;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Engine;
using Centazio.Core.Misc;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Hosts.Aws;
using Centazio.Hosts.Self;
using Centazio.Test.Lib;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Tests.Commands;

public class HostEnginesTests {

  private static readonly SystemName SYSTEM = new(nameof(HostEnginesTests));
  private static readonly LifecycleStage SOURCE_STAGE = new("source");
  private static readonly SystemEntityTypeName OBJECT = new("object");
  
  [Test] public async Task Test_self_host_notifier_is_usable_after_engine_initialisation() {
    // the notifier was created with `using var` during service registration, so it was disposed (its queue closed)
    //    before the host started and the first notification threw `ChannelClosedException`
    var prov = await new SelfHostCentazioEngineAdapter(await F.Settings(), [CentazioConstants.DEFAULT_ENVIRONMENT]).Init(TestFunctions());
    var (notifier, runner) = ((InProcessChangesNotifier) prov.GetRequiredService<IChangesNotifier>(), prov.GetRequiredService<IFunctionRunner>());
    var func = new TriggeredFunction(prov.GetRequiredService<ICtlRepository>(), prov.GetRequiredService<TimeProvider>());
    notifier.Init([func]);
    
    await notifier.Notify(SYSTEM, SOURCE_STAGE, [OBJECT]);
    await notifier.ProcessPending(runner);
    
    Assert.That(func.RunCount, Is.EqualTo(1));
  }
  
  [Test] public async Task Test_aws_local_test_engine_throws_function_exceptions() {
    var engine = AwsFunctionLocalTestCommand.CreateEngine(await F.Settings(), [CentazioConstants.DEFAULT_ENVIRONMENT]);
    Assert.ThrowsAsync<Exception>(() => RunThrowingFunction(engine, new("throwing-local")));
  }
  
  [Test] public async Task Test_aws_lambda_engine_returns_function_exceptions_as_errors() {
    var engine = AwsHost.CreateEngine(await F.Settings(), [CentazioConstants.DEFAULT_ENVIRONMENT]);
    var results = await RunThrowingFunction(engine, new("throwing-lambda"));
    Assert.That(results.OpResults.Single().Result, Is.TypeOf<ErrorOperationResult>());
  }

  // each test uses its own stage, as function state persists in the default (sqlite file) ctl repository
  private async Task<FunctionRunResults> RunThrowingFunction(CentazioEngine engine, LifecycleStage stage) {
    // skips the EventBridge rule setup the lambda notifier does on the first run of a function
    Environment.SetEnvironmentVariable(AwsEventBridgeChangesNotifier.ENV_SETUP, "1");
    // the lambda runtime always sets the region, which the notifier's aws clients require
    var region = Environment.GetEnvironmentVariable("AWS_REGION");
    if (region is null) Environment.SetEnvironmentVariable("AWS_REGION", (await F.Settings()).AwsSettings.Region);
    try {
      var prov = await engine.Init(TestFunctions());
      var func = new ThrowingFunction(stage, prov.GetRequiredService<ICtlRepository>(), prov.GetRequiredService<TimeProvider>());
      return await prov.GetRequiredService<IFunctionRunner>().RunFunction(func, []);
    } finally { 
      Environment.SetEnvironmentVariable(AwsEventBridgeChangesNotifier.ENV_SETUP, null);
      Environment.SetEnvironmentVariable("AWS_REGION", region);
    }
  }
  
  private static List<Type> TestFunctions() => IntegrationsAssemblyInspector.GetRequiredCentazioFunctions(ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), []);
  
  class TriggeredFunction(ICtlRepository ctl, TimeProvider clock) : AbstractFunction<ReadOperationConfig>(SYSTEM, new("triggered"), ctl, clock) {
    
    public int RunCount { get; private set; }
    
    protected override FunctionConfig GetFunctionConfiguration() => new([
      new ReadOperationConfig(OBJECT, CronExpressionsHelper.EverySecond(), F.GetEmptyResult) { Triggers = [new(SYSTEM, SOURCE_STAGE, OBJECT)] }
    ]);

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
      RunCount++;
      return Task.FromResult<OperationResult>(ReadOperationResult.EmptyResult());
    }
  }
  
  class ThrowingFunction(LifecycleStage stage, ICtlRepository ctl, TimeProvider clock) : AbstractFunction<ReadOperationConfig>(SYSTEM, stage, ctl, clock) {
    
    protected override FunctionConfig GetFunctionConfiguration() => new([
      new ReadOperationConfig(OBJECT, CronExpressionsHelper.EverySecond(), F.GetEmptyResult)
    ]);

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception(nameof(ThrowingFunction));
  }
}
