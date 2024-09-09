using System.Text.Json;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Read;

public class ReadOperationRunnerTests {

  private TestingStagedEntityStore store;
  private TestingCtlRepository repo;

  [SetUp] public void SetUp() {
    store = new TestingStagedEntityStore();
    repo = TestingFactories.CtlRepo();
  }
  
  [TearDown] public async Task TearDown() {
    await store.DisposeAsync();
    await repo.DisposeAsync();
  } 
  
  [Test] public async Task Test_FailedRead_operations_are_not_staged() {
    var runner = TestingFactories.ReadRunner(store);
    var actual = await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Error, TestingFactories.TestingSingleReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new ErrorReadOperationResult(""),
        actual,
        new SystemState(EOperationResult.Error.ToString(), EOperationResult.Error.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle) );
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = TestingFactories.ReadRunner(store);
    var opcfg = await CreateReadOpStateAndConf(EOperationResult.Success, TestingFactories.TestingEmptyReadOperationImplementation);
    var actual = await runner.RunOperation(UtcDate.UtcNow, opcfg);
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new EmptyReadOperationResult(""),
        actual,
        new SystemState(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle));
  }
  
  [Test] public async Task Test_valid_Single_results_are_staged() {
    var runner = TestingFactories.ReadRunner(store);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(
        UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Success, TestingFactories.TestingSingleReadOperationImplementation));

    var staged = store.Contents.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, staged.Data, staged.Checksum)));
    ValidateResult(
        new SingleRecordReadOperationResult(actual.Payload, ""),
        actual,
        new SystemState(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle) );
  }
  
  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = TestingFactories.ReadRunner(store);
    var actual = (ListRecordsReadOperationResult) await runner.RunOperation(
        UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Success, TestingFactories.TestingListReadOperationImplementation));
    
    var staged = store.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, s.Data, s.Checksum))));
    ValidateResult(
        new ListRecordsReadOperationResult(actual.PayloadList, ""),
        actual,
        new SystemState(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle) );
  }
  
  [Test] public void Test_results_cannot_be_invalid_PayloadLength() {
    Assert.Throws<ArgumentNullException>(() => _ = new ListRecordsReadOperationResult([], ""));
    Assert.Throws<ArgumentNullException>(() => _ = new ListRecordsReadOperationResult([""], ""));
    Assert.Throws<ArgumentNullException>(() => _ = new ListRecordsReadOperationResult([null!], ""));
  }
  
  private void ValidateResult(IOperationResult expected, IOperationResult actual, SystemState expss) {
    Assert.That(JsonSerializer.Serialize(actual), Is.EqualTo(JsonSerializer.Serialize(expected)));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss));
  }
  
  private async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(EOperationResult result, Func<OperationStateAndConfig<ReadOperationConfig>, Task<ReadOperationResult>> Impl) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), result.ToString()), 
        new (result.ToString(), TestingDefaults.CRON_EVERY_SECOND, DateTime.MinValue, Impl));
  
}