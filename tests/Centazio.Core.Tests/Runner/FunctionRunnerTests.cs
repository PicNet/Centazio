using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class FunctionRunnerTests {

  private const string NAME = nameof(FunctionRunnerTests);
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    var repo = TestingFactories.CtlRepo();
    var results = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("success"));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    var repo = TestingFactories.CtlRepo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle));
    
    var results = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("inactive"));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var repo = TestingFactories.CtlRepo();
    var results = await new FunctionRunner<ReadOperationConfig>(new SimpleFunction(count), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("success"));
    Assert.That(results.OpResults, Is.EquivalentTo(Enumerable.Range(0, count).Select(idx => new EmptyOperationResult(EOperationResult.Success, idx.ToString()))));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    var repo = TestingFactories.CtlRepo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow));
    
    var results = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("not idle"));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    var repo = TestingFactories.CtlRepo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow.AddHours(-1)));
    var results = await new FunctionRunner<ReadOperationConfig>(new SimpleFunction(1), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(results.Message, Is.EqualTo("success"));
    Assert.That(results.OpResults, Is.EquivalentTo(new OperationResult[] { new EmptyOperationResult(EOperationResult.Success, "0") }));
  }

  record EmptyFunctionConfig() : FunctionConfig<ReadOperationConfig>(NAME, NAME, new List<ReadOperationConfig> { 
    new(NAME, TestingDefaults.CRON_EVERY_SECOND, DateTime.MinValue, _ => Task.FromResult(new EmptyOperationResult(EOperationResult.Success, "") as OperationResult))
  });
  
  class EmptyFunction : IFunction {
    public Task<IEnumerable<OperationResult>> Run(DateTime start) => Task.FromResult<IEnumerable<OperationResult>>(Array.Empty<OperationResult>());
  }
  
  class SimpleFunction(int results) : IFunction {
    public Task<IEnumerable<OperationResult>> Run(DateTime start) => 
        Task.FromResult(Enumerable.Range(0, results)
        .Select(idx => new EmptyOperationResult(EOperationResult.Success, idx.ToString()) as OperationResult));
  }
}