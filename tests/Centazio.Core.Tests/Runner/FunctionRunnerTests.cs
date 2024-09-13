using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class FunctionRunnerTests {

  private const string NAME = nameof(FunctionRunnerTests);
  
  private readonly DoNothingOpRunner oprunner = new();
  private readonly EmptyFunction emptufunc = new();
  private TestingCtlRepository repo;
  
  [SetUp] public void SetUp() {
    repo = TestingFactories.CtlRepo();
  }
  
  [TearDown] public async Task TearDown() {
    await repo.DisposeAsync();
  }
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    Assert.That(repo.Systems, Is.Empty);
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("success"));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle));
    
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("inactive"));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new SimpleFunction(count), oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("success"));
    Assert.That(results.OpResults, Is.EquivalentTo(Enumerable.Range(0, count).Select(_ => new EmptyReadOperationResult(""))));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow));
    
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("not idle"));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow.AddHours(-1)));
    var results = await new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new SimpleFunction(1), oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("success"));
    Assert.That(results.OpResults, Is.EquivalentTo(new[] { new EmptyReadOperationResult("") }));
  }

  record EmptyFunctionConfig() : FunctionConfig<ReadOperationConfig>(NAME, NAME, new List<ReadOperationConfig> { 
    new(NAME, TestingDefaults.CRON_EVERY_SECOND, DateTime.MinValue, _ => Task.FromResult(new EmptyReadOperationResult("") as ReadOperationResult))
  });
  
  class EmptyFunction : IFunction<ReadOperationConfig, ReadOperationResult> {

    public FunctionConfig<ReadOperationConfig> Config { get; } = new EmptyFunctionConfig();
    
    public async Task<IEnumerable<ReadOperationResult>> RunOperation(IOperationRunner<ReadOperationConfig, ReadOperationResult> runner, ICtlRepository ctl) {
      var state = await ctl.GetSystemState(Config.System, Config.Stage) ?? throw new Exception();
      Assert.That(state.Status, Is.EqualTo(ESystemStateStatus.Running));
      return Array.Empty<ReadOperationResult>();
    }

  }
  
  class SimpleFunction(int results) : IFunction<ReadOperationConfig, ReadOperationResult> {

    public FunctionConfig<ReadOperationConfig> Config { get; } = new EmptyFunctionConfig();
    
    public async Task<IEnumerable<ReadOperationResult>> RunOperation(IOperationRunner<ReadOperationConfig, ReadOperationResult> runner, ICtlRepository ctl) {
      var state = await ctl.GetSystemState(Config.System, Config.Stage) ?? throw new Exception();
      Assert.That(state.Status, Is.EqualTo(ESystemStateStatus.Running));
      return Enumerable.Range(0, results).Select(_ => new EmptyReadOperationResult(""));
    }

  }
  
  class DoNothingOpRunner : IOperationRunner<ReadOperationConfig, ReadOperationResult> {

    public Task<ReadOperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) => throw new Exception();
    public ReadOperationResult BuildErrorResult(OperationStateAndConfig<ReadOperationConfig> op, Exception ex) => throw new Exception();

  }
}