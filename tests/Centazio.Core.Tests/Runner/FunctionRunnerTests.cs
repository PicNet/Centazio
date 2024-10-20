using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class FunctionRunnerTests {

  private readonly DoNothingOpRunner oprunner = new();
  private readonly EmptyFunction emptufunc = new();
  private TestingInMemoryCtlRepository repo;
  
  [SetUp] public void SetUp() {
    repo = F.CtlRepo();
  }
  
  [TearDown] public async Task TearDown() {
    await repo.DisposeAsync();
  }
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    Assert.That(repo.Systems, Is.Empty);
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, false, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()).ToBase());
    
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, false, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(InactiveFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new SimpleFunction(count), oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Enumerable.Range(0, count).Select(_ => new EmptyReadOperationResult())));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow).ToBase());
    
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(AlreadyRunningFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    repo.Systems.Add((C.System1Name, LifecycleStage.Defaults.Read), new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow.AddHours(-1)).ToBase());
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new SimpleFunction(1), oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState.Dto(C.System1Name, LifecycleStage.Defaults.Read, true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow).ToBase()));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(new[] { new EmptyReadOperationResult() }));
  }
  
  record EmptyFunctionConfig() : FunctionConfig<ReadOperationConfig>(C.System1Name, LifecycleStage.Defaults.Read, [
    new(C.SystemEntityName, TestingDefaults.CRON_EVERY_SECOND, new EmptyResults())
  ]) {
    
    private class EmptyResults : IGetObjectsToStage {
      public Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) => Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult());
    }
  }
  
  class EmptyFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult> {

    public override FunctionConfig<ReadOperationConfig> Config { get; } = new EmptyFunctionConfig();
    
    public override async Task<List<ReadOperationResult>> RunFunctionOperations(IOperationRunner<ReadOperationConfig, ReadOperationResult> runner, ICtlRepository ctl) {
      var state = await ctl.GetSystemState(Config.System, Config.Stage) ?? throw new Exception();
      Assert.That(state.Status, Is.EqualTo(ESystemStateStatus.Running));
      return [];
    }

  }
  
  class SimpleFunction(int results) : AbstractFunction<ReadOperationConfig, ReadOperationResult> {

    public override FunctionConfig<ReadOperationConfig> Config { get; } = new EmptyFunctionConfig();
    
    public override async Task<List<ReadOperationResult>> RunFunctionOperations(IOperationRunner<ReadOperationConfig, ReadOperationResult> runner, ICtlRepository ctl) {
      var state = await ctl.GetSystemState(Config.System, Config.Stage) ?? throw new Exception();
      Assert.That(state.Status, Is.EqualTo(ESystemStateStatus.Running));
      return Enumerable.Range(0, results).Select(_ => (ReadOperationResult) new EmptyReadOperationResult()).ToList();
    }

  }
  
  class DoNothingOpRunner : IOperationRunner<ReadOperationConfig, ReadOperationResult> {

    public Task<ReadOperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception();
    public ReadOperationResult BuildErrorResult(OperationStateAndConfig<ReadOperationConfig> op, Exception ex) => throw new Exception();

  }
}