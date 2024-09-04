using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using centazio.core.Runner;
using Centazio.Core.Tests;

namespace centazio.core.tests.Runner;

public class FunctionRunnerTests {

  private const string NAME = nameof(FunctionRunnerTests);
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    var repo = TestingFactories.Repo();
    var result = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] completed with empty results"));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    var repo = TestingFactories.Repo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle));
    
    var result = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] inactive"));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var repo = TestingFactories.Repo();
    var result = await new FunctionRunner<ReadOperationConfig>(new SimpleFunction(count), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo(String.Join("\n", Enumerable.Range(0, count).Select(idx => idx.ToString()))));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    var repo = TestingFactories.Repo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow));
    
    var result = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] is not idle"));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    var repo = TestingFactories.Repo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow.AddHours(-1)));
    var result = await new FunctionRunner<ReadOperationConfig>(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] completed with empty results"));
  }

  record EmptyFunctionConfig() : FunctionConfig<ReadOperationConfig>(NAME, NAME, new List<ReadOperationConfig> { 
    new(NAME, "* * * * *", (_, _) => Task.FromResult(new EmptyOperationResult(EOperationResult.Success, "") as OperationResult))
  });
  
  class EmptyFunction : IFunction {
    public Task<IEnumerable<OperationResult>> Run(SystemState state, DateTime start) => Task.FromResult<IEnumerable<OperationResult>>(Array.Empty<OperationResult>());
  }
  
  class SimpleFunction(int results) : IFunction {
    public Task<IEnumerable<OperationResult>> Run(SystemState state, DateTime start) => 
        Task.FromResult(Enumerable.Range(0, results)
        .Select(idx => new EmptyOperationResult(EOperationResult.Success, idx.ToString()) as OperationResult));
  }
}