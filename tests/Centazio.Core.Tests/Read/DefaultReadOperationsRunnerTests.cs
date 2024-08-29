using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;
using F = Centazio.Core.Tests.Read.ReadTestFactories;

namespace Centazio.Core.Tests.Read;

public class DefaultReadOperationRunnerTests {

  private TestingStagedEntityStore store;
  private TestingCtlRepository repo;

  [SetUp] public void SetUp() {
    store = new TestingStagedEntityStore();
    repo = F.Repo();
  }
  
  [TearDown] public async Task TearDown() {
    await store.DisposeAsync();
    await repo.DisposeAsync();
  } 
  
  [Test] public async Task Test_FailedRead_operations_are_not_staged() {
    var runner = F.Runner(store, repo);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.FailedRead, F.TestingSingleReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new SingleRecordReadOperationResult(EOperationReadResult.FailedRead, "", actual.Payload),
        actual,
        new SystemState(EOperationReadResult.FailedRead.ToString(), EOperationReadResult.FailedRead.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.FailedRead.ToString(), true, UtcDate.UtcNow, 
            EOperationReadResult.FailedRead, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = F.Runner(store, repo);
    var actual = await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.Success, F.TestingEmptyReadOperationImplementation));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new EmptyReadOperationResult(EOperationReadResult.Success, ""),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, 
           EOperationReadResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:0 ));
  }
  
  [Test] public async Task Test_valid_Single_results_are_staged() {
    var runner = F.Runner(store, repo);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.Success, F.TestingSingleReadOperationImplementation));
    
    var staged = store.Contents.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), UtcDate.UtcNow, staged.Data)));
    ValidateResult(
        new SingleRecordReadOperationResult(EOperationReadResult.Success, "", actual.Payload),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, 
            EOperationReadResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = F.Runner(store, repo);
    var actual = (ListRecordReadOperationResult) await runner.RunOperation(UtcDate.UtcNow, await CreateReadOpStateAndConf(EOperationReadResult.Success, F.TestingListReadOperationImplementation));
    
    var staged = store.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), UtcDate.UtcNow, s.Data))));
    ValidateResult(
        new ListRecordReadOperationResult(EOperationReadResult.Success, "", actual.PayloadList),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, ESystemStateStatus.Idle),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, UtcDate.UtcNow, 
            EOperationReadResult.Success, EOperationAbortVote.Continue, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, "*", LastPayLoadLength: staged.Count) { LastPayLoadType = EPayloadType.List } );
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
        new (result.ToString(), new ("* * * * *"), Impl));
  
}