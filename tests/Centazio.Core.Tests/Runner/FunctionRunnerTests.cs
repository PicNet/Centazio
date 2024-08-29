using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Core.Runner;
using Centazio.Core.Tests.Read;

namespace Centazio.Core.Tests.Runner;

public class FunctionRunnerTests {

  private const string NAME = nameof(FunctionRunnerTests);
  
  [Test] public async Task Test_run_functions_creates_state_if_it_does_not_exist() {
    var repo = ReadTestFactories.Repo();
    var result = await new FunctionRunner(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] completed with empty results"));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    var repo = ReadTestFactories.Repo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle));
    
    var result = await new FunctionRunner(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] inactive"));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var repo = ReadTestFactories.Repo();
    var result = await new FunctionRunner(new SimpleFunction(count), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo(String.Join("\n", Enumerable.Range(0, count).Select(idx => idx.ToString()))));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    var repo = ReadTestFactories.Repo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow));
    
    var result = await new FunctionRunner(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] is not idle"));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    var repo = ReadTestFactories.Repo();
    repo.Systems.Add((NAME, NAME), new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running, LastStarted: UtcDate.UtcNow.AddHours(-1)));
    var result = await new FunctionRunner(new EmptyFunction(), new EmptyFunctionConfig(), repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo(new SystemState(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.Objects, Is.Empty);
    Assert.That(result, Is.EqualTo("function [FunctionRunnerTests/FunctionRunnerTests] completed with empty results"));
  }

  record EmptyFunctionConfig() : BaseFunctionConfig(NAME, NAME) {
    public override void Validate() {}
  }
  
  record SimpleFunctionOperationResult(string Message) : BaseFunctionOperationResult(Message);
  
  class EmptyFunction : IFunction {
    public Task<IEnumerable<BaseFunctionOperationResult>> Run(SystemState state, DateTime start) => Task.FromResult<IEnumerable<BaseFunctionOperationResult>>(Array.Empty<BaseFunctionOperationResult>());
  }
  
  class SimpleFunction(int results) : IFunction {
    public Task<IEnumerable<BaseFunctionOperationResult>> Run(SystemState state, DateTime start) => 
        Task.FromResult(Enumerable.Range(0, results)
        .Select(idx => new SimpleFunctionOperationResult(idx.ToString()) as BaseFunctionOperationResult));
  }
}