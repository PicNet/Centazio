using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;

namespace Centazio.Core.Tests.Read;

public class DefaultReadOperationRunnerTests {

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
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.FailedRead, TestingFactories.TestingSingleReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new SingleRecordReadOperationResult(EOperationReadResult.FailedRead, "", actual.Payload),
        actual,
        new SystemState(EOperationReadResult.FailedRead.ToString(), EOperationReadResult.FailedRead.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.FailedRead.ToString(), true, UtcDate.UtcNow, 
            EOperationReadResult.FailedRead, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.Success, TestingFactories.TestingEmptyReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new EmptyReadOperationResult(EOperationReadResult.Success, ""),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, 
           EOperationReadResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:0 ));
  }
  
  [Test] public async Task Test_valid_Single_results_are_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.Success, TestingFactories.TestingSingleReadOperationImplementation));
    
    var staged = store.Contents.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), UtcDate.UtcNow, staged.Data, staged.Checksum)));
    ValidateResult(
        new SingleRecordReadOperationResult(EOperationReadResult.Success, "", actual.Payload),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, 
            EOperationReadResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = TestingFactories.Runner(store, repo);
    var actual = (ListRecordReadOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.Success, TestingFactories.TestingListReadOperationImplementation));
    
    var staged = store.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), UtcDate.UtcNow, s.Data, s.Checksum))));
    ValidateResult(
        new ListRecordReadOperationResult(EOperationReadResult.Success, "", actual.PayloadList),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, 
            EOperationReadResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength: staged.Count) { LastPayLoadType = EPayloadType.List } );
  }
  
  [Test] public void Test_results_cannot_be_invalid_PayloadLength() {
    Assert.Throws<ArgumentException>(() => _ = new SingleRecordReadOperationResult(EOperationReadResult.Success, "", ""));
    Assert.Throws<ArgumentException>(() => _ = new ListRecordReadOperationResult(EOperationReadResult.Success, "", new (new List<string>())));
    Assert.Throws<ArgumentException>(() => _ = new ListRecordReadOperationResult(EOperationReadResult.Success, "", new (new List<string> { "1", "", null! })));
    
    Assert.That(new SingleRecordReadOperationResult(EOperationReadResult.Success, "", "*"), Is.Not.Null);
    Assert.That(new ListRecordReadOperationResult(EOperationReadResult.Success, "", new(new List<string> { "1", "2" })), Is.Not.Null);
    Assert.That(new EmptyReadOperationResult(EOperationReadResult.Success, ""), Is.Not.Null);
  }
  
  [Test] public void Test_results_cannot_be_uknown_Result() {
    Assert.Throws<ArgumentException>(() => _ = new SingleRecordReadOperationResult(EOperationReadResult.Unknown, "", "*"));
    Assert.Throws<ArgumentException>(() => _ = new ListRecordReadOperationResult(EOperationReadResult.Unknown, "", new (new List<string> { "*" })));
    
    Assert.That(new SingleRecordReadOperationResult(EOperationReadResult.Success, "", "*"), Is.Not.Null);
    Assert.That(new ListRecordReadOperationResult(EOperationReadResult.Success, "", new(new List<string> { "1", "2" })), Is.Not.Null);
    Assert.That(new EmptyReadOperationResult(EOperationReadResult.Success, ""), Is.Not.Null);
  }
  
  private void ValidateResult(ReadOperationResult expected, ReadOperationResult actual, SystemState expss, ObjectState expos) {
    var actualos = repo.Objects.Single().Value;
    expos = expos with { System = expss.System, Stage = expss.Stage, LastRunMessage = actualos.LastRunMessage };
    
    Assert.That(actual, Is.EqualTo(expected));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss));
    Assert.That(actualos, Is.EqualTo(expos));
  }
  
  private async Task<ReadOperationStateAndConfig> CreateReadOpStateAndConf(EOperationReadResult result, Func<DateTime, ReadOperationStateAndConfig, Task<ReadOperationResult>> Impl) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), result.ToString()), 
        new (result.ToString(), new (new ("* * * * *")), Impl));
  
}