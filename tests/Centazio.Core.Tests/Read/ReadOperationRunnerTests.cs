using Centazio.Core.Ctl.Entities;
using centazio.core.Runner;

namespace Centazio.Core.Tests.Read;

public class ReadOperationRunnerTests {

  private TestingStagedEntityStore store;
  private TestingCtlRepository repo;

  [SetUp] public void SetUp() {
    store = new TestingStagedEntityStore();
    repo = TestingFactories.Repo();
  }
  
  [TearDown] public async Task TearDown() {
    await store.DisposeAsync();
    await repo.DisposeAsync();
  } 
  
  [Test] public async Task Test_FailedRead_operations_are_not_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = (SingleRecordOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Error, TestingFactories.TestingSingleReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new SingleRecordOperationResult(EOperationResult.Error, "", actual.Payload),
        actual,
        new SystemState(EOperationResult.Error.ToString(), EOperationResult.Error.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationResult.Error.ToString(), true, UtcDate.UtcNow, 
            EOperationResult.Error, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Success, TestingFactories.TestingEmptyReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new EmptyOperationResult(EOperationResult.Success, ""),
        actual,
        new SystemState(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationResult.Success.ToString(), true, UtcDate.UtcNow, 
           EOperationResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:0 ));
  }
  
  [Test] public async Task Test_valid_Single_results_are_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = (SingleRecordOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Success, TestingFactories.TestingSingleReadOperationImplementation));
    
    var staged = store.Contents.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, staged.Data, staged.Checksum)));
    ValidateResult(
        new SingleRecordOperationResult(EOperationResult.Success, "", actual.Payload),
        actual,
        new SystemState(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationResult.Success.ToString(), true, UtcDate.UtcNow, 
            EOperationResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = (ListRecordOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationResult.Success, TestingFactories.TestingListReadOperationImplementation));
    
    var staged = store.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), UtcDate.UtcNow, s.Data, s.Checksum))));
    ValidateResult(
        new ListRecordOperationResult(EOperationResult.Success, "", actual.PayloadList),
        actual,
        new SystemState(EOperationResult.Success.ToString(), EOperationResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationResult.Success.ToString(), true, UtcDate.UtcNow, 
            EOperationResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength: staged.Count) { LastPayLoadType = EPayloadType.List } );
  }
  
  [Test] public void Test_results_cannot_be_invalid_PayloadLength() {
    Assert.Throws<ArgumentException>(() => _ = new SingleRecordOperationResult(EOperationResult.Success, "", ""));
    Assert.Throws<ArgumentException>(() => _ = new ListRecordOperationResult(EOperationResult.Success, "", new (new List<string>())));
    Assert.Throws<ArgumentException>(() => _ = new ListRecordOperationResult(EOperationResult.Success, "", new (new List<string> { "1", "", null! })));
    
    Assert.That(new SingleRecordOperationResult(EOperationResult.Success, "", "*"), Is.Not.Null);
    Assert.That(new ListRecordOperationResult(EOperationResult.Success, "", new(new List<string> { "1", "2" })), Is.Not.Null);
    Assert.That(new EmptyOperationResult(EOperationResult.Success, ""), Is.Not.Null);
  }
  
  [Test] public void Test_results_cannot_be_uknown_Result() {
    Assert.Throws<ArgumentException>(() => _ = new SingleRecordOperationResult(EOperationResult.Unknown, "", "*"));
    Assert.Throws<ArgumentException>(() => _ = new ListRecordOperationResult(EOperationResult.Unknown, "", new (new List<string> { "*" })));
    
    Assert.That(new SingleRecordOperationResult(EOperationResult.Success, "", "*"), Is.Not.Null);
    Assert.That(new ListRecordOperationResult(EOperationResult.Success, "", new(new List<string> { "1", "2" })), Is.Not.Null);
    Assert.That(new EmptyOperationResult(EOperationResult.Success, ""), Is.Not.Null);
  }
  
  private void ValidateResult(OperationResult expected, OperationResult actual, SystemState expss, ObjectState expos) {
    var actualos = repo.Objects.Single().Value;
    expos = expos with { System = expss.System, Stage = expss.Stage, LastRunMessage = actualos.LastRunMessage };
    
    Assert.That(actual, Is.EqualTo(expected));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss));
    Assert.That(actualos, Is.EqualTo(expos));
  }
  
  private async Task<OperationStateAndConfig> CreateReadOpStateAndConf(EOperationResult result, Func<DateTime, OperationStateAndConfig, Task<OperationResult>> Impl) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), result.ToString()), 
        new (result.ToString(), new (new ("* * * * *")), Impl));
  
}