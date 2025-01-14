using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Read;

public class ReadOperationRunnerTests {

  private TestingStagedEntityRepository repository;
  private TestingInMemoryBaseCtlRepository repo;

  [SetUp] public void SetUp() {
    repository = new TestingStagedEntityRepository();
    repo = F.CtlRepo();
  }
  
  [TearDown] public async Task TearDown() {
    await repository.DisposeAsync();
    await repo.DisposeAsync();
  } 
  
  [Test] public async Task Test_FailedRead_operations_are_not_staged() {
    var runner = F.ReadRunner(repository);
    var actual = await runner.RunOperation(await CreateReadOpStateAndConf(EOperationResult.Error, GetListOrErrorResults));
    
    Assert.That(repository.Contents, Is.Empty);
    ValidateResult(new SystemState.Dto(EOperationResult.Error.ToString(), EOperationResult.Error.ToString(), true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new ErrorReadOperationResult(), actual);
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = F.ReadRunner(repository);
    var opcfg = await CreateReadOpStateAndConf(EOperationResult.Success, GetEmptyOrErrorResults);
    var actual = await runner.RunOperation(opcfg);
    
    Assert.That(repository.Contents, Is.Empty);
    ValidateResult(new SystemState.Dto(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new EmptyReadOperationResult(), actual);
  }

  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = F.ReadRunner(repository);
    var actual = (ListReadOperationResult) await runner.RunOperation(await CreateReadOpStateAndConf(EOperationResult.Success, GetListOrErrorResults));
    
    var staged = repository.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity.Dto(s.Id, EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, s.Data, s.StagedEntityChecksum).ToBase())));
    ValidateResult(new SystemState.Dto(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new ListReadOperationResult(actual.PayloadList, UtcDate.UtcNow), actual);
  }
  
  [Test] public void Test_results_cannot_be_invalid_PayloadLength() {
    Assert.Throws<ArgumentNullException>(() => _ = new ListReadOperationResult([], UtcDate.UtcNow));
    Assert.Throws<ArgumentNullException>(() => _ = new ListReadOperationResult([String.Empty], UtcDate.UtcNow));
    Assert.Throws<ArgumentNullException>(() => _ = new ListReadOperationResult([null!], UtcDate.UtcNow));
  }
  
  private void ValidateResult(SystemState.Dto expss, OperationResult expected, OperationResult actual) {
    Assert.That(Json.Serialize(actual), Is.EqualTo(Json.Serialize(expected)));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss.ToBase()));
  }
  
  private async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(EOperationResult result, GetUpdatesAfterCheckpointHandler impl) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(new(result.ToString()), new(result.ToString())), new SystemEntityTypeName(result.ToString()), UtcDate.UtcNow),
        new BaseFunctionConfig(),
        new (new SystemEntityTypeName(result.ToString()), TestingDefaults.CRON_EVERY_SECOND, impl), DateTime.MinValue);
  
  private async Task<ReadOperationResult> GetEmptyOrErrorResults(OperationStateAndConfig<ReadOperationConfig> config) => await GetResultsImpl(config) ?? new EmptyReadOperationResult();
  private async Task<ReadOperationResult> GetListOrErrorResults(OperationStateAndConfig<ReadOperationConfig> config) => await GetResultsImpl(config) ?? new ListReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList(), UtcDate.UtcNow);
  private Task<ReadOperationResult?> GetResultsImpl(OperationStateAndConfig<ReadOperationConfig> config) => Task.FromResult<ReadOperationResult?>(
      Enum.Parse<EOperationResult>(config.OpConfig.Object) == EOperationResult.Error ? 
          new ErrorReadOperationResult() : null);

}