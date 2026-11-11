using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class FunctionRunnerTests {
  
  private FakeTimeProvider clock;
  private TestingInMemoryBaseCtlRepository repo;
  private EmptyFunction emptufunc;
  
  [SetUp] public void SetUp() {
    clock = F.Clock();
    repo = F.CtlRepo(clock);
    emptufunc = new(repo, clock);
  }
  
  [TearDown] public async Task TearDown() {
    await repo.DisposeAsync();
  }
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    Assert.That(repo.Systems, Is.Empty);
    var results = await F.RunFunc(clock, emptufunc, repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Idle.ToString(), clock.UtcNow(), clock.UtcNow()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add(new (C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, false, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Idle.ToString()).ToBase());
    
    var results = await F.RunFunc(clock, emptufunc, repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, false, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Idle.ToString()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(InactiveFunctionRunResults)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var results = await F.RunFunc(clock, new SimpleFunction(repo, clock, count), repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Idle.ToString(), clock.UtcNow(), clock.UtcNow()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults)));
    Assert.That(results.OpResults.Select(r => r.Result), Is.EquivalentTo(Enumerable.Range(0, count).Select(_ => new EmptyReadOperationResult())));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add(new (C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Running.ToString(), laststart: clock.UtcNow()).ToBase());
    
    var results = await F.RunFunc(clock, emptufunc, repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Running.ToString(), laststart: clock.UtcNow()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(AlreadyRunningFunctionRunResults)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    repo.Systems.Add(new (C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Running.ToString(), laststart: clock.UtcNow().AddHours(-1)).ToBase());
    var results = await F.RunFunc(clock, new SimpleFunction(repo, clock, 1), repo);
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, clock.UtcNow(), clock.UtcNow(), ESystemStateStatus.Idle.ToString(), clock.UtcNow(), clock.UtcNow()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults)));
    Assert.That(results.OpResults.Single().Result, Is.EqualTo(new EmptyReadOperationResult()));
  }
  
  [TestCase(true, null, true)]
  [TestCase(false, null, false)]
  [TestCase(true, false, false)]
  [TestCase(false, true, true)]
  public async Task Test_ThrowExceptions_defaults_to_the_runner_and_config_overrides(bool runnerdefault, bool? configured, bool expthrows) {
    var func = new ThrowingFunction(repo, clock, configured);
    var runner = new FunctionRunner(repo, await F.Settings(), clock, runnerdefault);
    
    if (expthrows) {
      Assert.ThrowsAsync<Exception>(() => runner.RunFunction(func, []));
      return;
    }
    var results = await runner.RunFunction(func, []);
    Assert.That(results.OpResults.Single().Result, Is.TypeOf<ErrorOperationResult>());
  }
  
  record EmptyFunctionConfig() : FunctionConfig([
    new ReadOperationConfig(C.SystemEntityName, TestingDefaults.CRON_EVERY_SECOND, _ => Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult()))
  ]);
  
  class EmptyFunction(ICtlRepository ctl, TimeProvider clock) : AbstractFunction<ReadOperationConfig>(C.System1Name, LifecycleStage.Defaults.Read, ctl, clock) {

    private readonly ICtlRepository ctlrepo = ctl;
    protected override FunctionConfig GetFunctionConfiguration() => new EmptyFunctionConfig();

    public override async Task RunFunctionOperations(SystemState state1, List<FunctionTrigger> triggers, List<OpResultAndObject> runningresults, bool throws) {
      var state2 = await ctlrepo.GetSystemState(System, Stage) ?? throw new Exception();
      Assert.That(state2.Status, Is.EqualTo(ESystemStateStatus.Running));
      Assert.That(state1, Is.EqualTo(state2));
    }

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception();

  }
  
  class SimpleFunction(ICtlRepository ctl, TimeProvider clock, int results) : AbstractFunction<ReadOperationConfig>(C.System1Name, LifecycleStage.Defaults.Read, ctl, clock) {

    private readonly ICtlRepository ctlrepo = ctl;
    protected override FunctionConfig GetFunctionConfiguration() => new EmptyFunctionConfig();

    public override async Task RunFunctionOperations(SystemState state1, List<FunctionTrigger> triggers, List<OpResultAndObject> runningresults, bool throws) {
      var state2 = await ctlrepo.GetSystemState(System, Stage) ?? throw new Exception();
      Assert.That(state2.Status, Is.EqualTo(ESystemStateStatus.Running));
      Assert.That(state1, Is.EqualTo(state2));
      runningresults.AddRange(Enumerable.Range(0, results).Select(_ => new OpResultAndObject(C.SystemEntityName, new EmptyReadOperationResult())));
    }

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception();

  }
  
  class ThrowingFunction(ICtlRepository ctl, TimeProvider clock, bool? throws) : AbstractFunction<ReadOperationConfig>(C.System1Name, LifecycleStage.Defaults.Read, ctl, clock) {
    
    protected override FunctionConfig GetFunctionConfiguration() => new EmptyFunctionConfig { ThrowExceptions = throws };
    
    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception(nameof(ThrowingFunction));
    
  }
}