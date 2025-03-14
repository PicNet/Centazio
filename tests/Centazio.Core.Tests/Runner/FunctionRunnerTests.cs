using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class FunctionRunnerTests {
  
  private TestingInMemoryBaseCtlRepository repo;
  private EmptyFunction emptufunc;
  
  [SetUp] public void SetUp() {
    repo = F.CtlRepo();
    emptufunc = new(repo);
  }
  
  [TearDown] public async Task TearDown() {
    await repo.DisposeAsync();
  }
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    Assert.That(repo.Systems, Is.Empty);
    var results = await F.RunFunc(emptufunc, ctl: repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, false, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()).ToBase());
    
    var results = await F.RunFunc(emptufunc, ctl: repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, false, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(InactiveFunctionRunResults)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var results = (await F.RunFunc(new SimpleFunction(repo, count), ctl: repo));
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults)));
    Assert.That(results.OpResults.Select(r => r.Result), Is.EquivalentTo(Enumerable.Range(0, count).Select(_ => new EmptyReadOperationResult())));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow).ToBase());
    
    var results = await F.RunFunc(emptufunc, ctl: repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(AlreadyRunningFunctionRunResults)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    repo.Systems.Add((C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow.AddHours(-1)).ToBase());
    var results = await F.RunFunc(new SimpleFunction(repo, 1), ctl: repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults)));
    Assert.That(results.OpResults.Single().Result, Is.EqualTo(new EmptyReadOperationResult()));
  }
  
  record EmptyFunctionConfig() : FunctionConfig<ReadOperationConfig>([
    new(C.SystemEntityName, TestingDefaults.CRON_EVERY_SECOND, _ => Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult()))
  ]);
  
  class EmptyFunction(ICtlRepository ctl) : AbstractFunction<ReadOperationConfig>(C.System1Name, LifecycleStage.Defaults.Read, ctl) {

    private readonly ICtlRepository ctlrepo = ctl;
    
    public override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new EmptyFunctionConfig();

    public override async Task<List<OpResultAndObject>> RunFunctionOperations(SystemState state1) {
      var state2 = await ctlrepo.GetSystemState(System, Stage) ?? throw new Exception();
      Assert.That(state2.Status, Is.EqualTo(ESystemStateStatus.Running));
      Assert.That(state1, Is.EqualTo(state2));
      return [];
    }

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception();

  }
  
  class SimpleFunction(ICtlRepository ctl, int results) : AbstractFunction<ReadOperationConfig>(C.System1Name, LifecycleStage.Defaults.Read, ctl) {

    private readonly ICtlRepository ctlrepo = ctl;
    
    public override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new EmptyFunctionConfig();

    public override async Task<List<OpResultAndObject>> RunFunctionOperations(SystemState state1) {
      var state2 = await ctlrepo.GetSystemState(System, Stage) ?? throw new Exception();
      Assert.That(state2.Status, Is.EqualTo(ESystemStateStatus.Running));
      Assert.That(state1, Is.EqualTo(state2));
      return Enumerable.Range(0, results).Select(_ => new OpResultAndObject(C.SystemEntityName, new EmptyReadOperationResult())).ToList();
    }

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception();

  }
}