﻿using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Tests.IntegrationTests;
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
    var results = await new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo((SystemState) new SystemState.Dto(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.GetObjects<ExternalEntityType>(), Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_inactive_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((NAME, NAME), (SystemState) new SystemState.Dto(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()));
    
    var results = await new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo((SystemState) new SystemState.Dto(NAME, NAME, false, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString())));
    Assert.That(repo.GetObjects<ExternalEntityType>(), Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(InactiveFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_run_functions_with_multiple_results() {
    var count = 10;
    var results = await new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(new SimpleFunction(count), oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo((SystemState) new SystemState.Dto(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.GetObjects<ExternalEntityType>(), Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Enumerable.Range(0, count).Select(_ => new EmptyReadOperationResult())));
  }
  
  [Test] public async Task Test_already_running_function_creates_valid_state_but_does_not_run() {
    repo.Systems.Add((NAME, NAME), (SystemState) new SystemState.Dto(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow));
    
    var results = await new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(emptufunc, oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo((SystemState) new SystemState.Dto(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow)));
    Assert.That(repo.GetObjects<ExternalEntityType>(), Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(AlreadyRunningFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(Array.Empty<OperationResult>()));
  }
  
  [Test] public async Task Test_stuck_running_function_runs_again() {
    repo.Systems.Add((NAME, NAME), (SystemState) new SystemState.Dto(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Running.ToString(), laststart: UtcDate.UtcNow.AddHours(-1)));
    var results = await new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(new SimpleFunction(1), oprunner, repo).RunFunction();
    
    Assert.That(repo.Systems.Values.Single(), Is.EqualTo((SystemState) new SystemState.Dto(NAME, NAME, true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString(), UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow)));
    Assert.That(repo.GetObjects<ExternalEntityType>(), Is.Empty);
    Assert.That(results.Message, Is.EqualTo(nameof(SuccessFunctionRunResults<ReadOperationResult>)));
    Assert.That(results.OpResults, Is.EquivalentTo(new[] { new EmptyReadOperationResult() }));
  }
  
  record EmptyFunctionConfig() : FunctionConfig<ReadOperationConfig, ExternalEntityType>(NAME, NAME, 
      new List<ReadOperationConfig> { new(Constants.ExternalEntityName, TestingDefaults.CRON_EVERY_SECOND, new EmptyResults()) }) {
    
    private class EmptyResults : IGetObjectsToStage {
      public Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) => Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult());
    }
  }
  
  class EmptyFunction : AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult> {

    public override FunctionConfig<ReadOperationConfig, ExternalEntityType> Config { get; } = new EmptyFunctionConfig();
    
    public override async Task<IEnumerable<ReadOperationResult>> RunFunctionOperations(IOperationRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult> runner, ICtlRepository ctl) {
      var state = await ctl.GetSystemState(Config.System, Config.Stage) ?? throw new Exception();
      Assert.That(state.Status, Is.EqualTo(ESystemStateStatus.Running));
      return Array.Empty<ReadOperationResult>();
    }

  }
  
  class SimpleFunction(int results) : AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult> {

    public override FunctionConfig<ReadOperationConfig, ExternalEntityType> Config { get; } = new EmptyFunctionConfig();
    
    public override async Task<IEnumerable<ReadOperationResult>> RunFunctionOperations(IOperationRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult> runner, ICtlRepository ctl) {
      var state = await ctl.GetSystemState(Config.System, Config.Stage) ?? throw new Exception();
      Assert.That(state.Status, Is.EqualTo(ESystemStateStatus.Running));
      return Enumerable.Range(0, results).Select(_ => new EmptyReadOperationResult());
    }

  }
  
  class DoNothingOpRunner : IOperationRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult> {

    public Task<ReadOperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> op) => throw new Exception();
    public ReadOperationResult BuildErrorResult(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> op, Exception ex) => throw new Exception();

  }
}