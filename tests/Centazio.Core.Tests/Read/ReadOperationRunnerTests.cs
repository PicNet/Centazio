using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Read;

public class ReadOperationRunnerTests {

  private TestingStagedEntityRepository repository;
  private TestingInMemoryCtlRepository repo;

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
    var actual = await runner.RunOperation(await CreateReadOpStateAndConf(EOperationResult.Error, new TestingSingleReadOperationImplementation()));
    
    Assert.That(repository.Contents, Is.Empty);
    ValidateResult(new SystemState.Dto(EOperationResult.Error.ToString(), EOperationResult.Error.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new ErrorReadOperationResult(), actual);
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = F.ReadRunner(repository);
    var opcfg = await CreateReadOpStateAndConf(EOperationResult.Success, new TestingEmptyReadOperationImplementation());
    var actual = await runner.RunOperation(opcfg);
    
    Assert.That(repository.Contents, Is.Empty);
    ValidateResult(new SystemState.Dto(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new EmptyReadOperationResult(), actual);
  }
  
  [Test] public async Task Test_valid_Single_results_are_staged() {
    var runner = F.ReadRunner(repository);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(await CreateReadOpStateAndConf(EOperationResult.Success, new TestingSingleReadOperationImplementation()));

    var staged = repository.Contents.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity.Dto(staged.Id, EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, staged.Data, staged.StagedEntityChecksum).ToBase()));
    ValidateResult(new SystemState.Dto(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new SingleRecordReadOperationResult(actual.Payload), actual);
  }
  
  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = F.ReadRunner(repository);
    var actual = (ListRecordsReadOperationResult) await runner.RunOperation(await CreateReadOpStateAndConf(EOperationResult.Success, new TestingListReadOperationImplementation()));
    
    var staged = repository.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity.Dto(s.Id, EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, s.Data, s.StagedEntityChecksum).ToBase())));
    ValidateResult(new SystemState.Dto(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle.ToString()), new ListRecordsReadOperationResult(actual.PayloadList), actual);
  }
  
  [Test] public void Test_results_cannot_be_invalid_PayloadLength() {
    Assert.Throws<ArgumentNullException>(() => _ = new ListRecordsReadOperationResult([]));
    Assert.Throws<ArgumentNullException>(() => _ = new ListRecordsReadOperationResult([String.Empty]));
    Assert.Throws<ArgumentNullException>(() => _ = new ListRecordsReadOperationResult([null!]));
  }
  
  private void ValidateResult(SystemState.Dto expss, OperationResult expected, OperationResult actual) {
    Assert.That(Json.Serialize(actual), Is.EqualTo(Json.Serialize(expected)));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss.ToBase()));
  }
  
  private async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(EOperationResult result, IGetObjectsToStage Impl) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), new SystemEntityTypeName(result.ToString())),
        new BaseFunctionConfig(),
        new (new SystemEntityTypeName(result.ToString()), TestingDefaults.CRON_EVERY_SECOND, Impl), DateTime.MinValue);
  
  private class TestingEmptyReadOperationImplementation : IGetObjectsToStage {
    public Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
      var result = Enum.Parse<EOperationResult>(config.OpConfig.Object);
      ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new EmptyReadOperationResult();
      return Task.FromResult(res);
    }
  }
  
  private class TestingSingleReadOperationImplementation : IGetObjectsToStage {
    public Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
      var result = Enum.Parse<EOperationResult>(config.OpConfig.Object); 
      ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new SingleRecordReadOperationResult(Guid.NewGuid().ToString());
      return Task.FromResult(res);
    }
  }
  
  private class TestingListReadOperationImplementation : IGetObjectsToStage {
    public Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
      var result = Enum.Parse<EOperationResult>(config.OpConfig.Object); 
      ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new ListRecordsReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList());
      return Task.FromResult(res); 
    }
  }
}