using centazio.core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Test.Lib;
using F = centazio.core.tests.Read.ReadTestFactories;

namespace centazio.core.tests.Read;

public class DefaultReadOperationRunnerTests {

  private TestingUtcDate utc;
  private TestingStagedEntityStore store;
  private TestingCtlRepository repo;

  [SetUp] public void SetUp() {
    utc = new TestingUtcDate();
    store = new TestingStagedEntityStore();
    repo = F.Repo(utc);
  }
  
  [TearDown] public async Task TearDown() {
    await store.DisposeAsync();
    await repo.DisposeAsync();
  } 
  
  [Test] public async Task Test_FailedRead_operations_are_not_staged() {
    var runner = F.Runner(utc, store, repo, F.TestingSingleReadOperationImplementation);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(utc.Now, await CreateReadOpStateAndConf(EOperationReadResult.FailedRead));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new SingleRecordReadOperationResult(EOperationReadResult.FailedRead, "", actual.Payload),
        actual,
        new SystemState(EOperationReadResult.FailedRead.ToString(), EOperationReadResult.FailedRead.ToString(), true, utc.Now),
        new ObjectState("*", "*", EOperationReadResult.FailedRead.ToString(), true, utc.Now, 
            EOperationReadResult.FailedRead, EOperationAbortVote.Continue, utc.Now, utc.Now, utc.Now, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = F.Runner(utc, store, repo, F.TestingEmptyReadOperationImplementation);
    var actual = await runner.RunOperation(utc.Now, await CreateReadOpStateAndConf(EOperationReadResult.Success));
    
    Assert.That(store.Contents, Is.Empty);
    ValidateResult(
        new EmptyReadOperationResult(EOperationReadResult.Success, ""),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, utc.Now),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, utc.Now, 
           EOperationReadResult.Success, EOperationAbortVote.Continue, utc.Now, utc.Now, utc.Now, "*", LastPayLoadLength:0 ));
  }
  
  [Test] public async Task Test_valid_Single_results_are_staged() {
    var runner = F.Runner(utc, store, repo, F.TestingSingleReadOperationImplementation);
    var actual = (SingleRecordReadOperationResult) await runner.RunOperation(utc.Now, await CreateReadOpStateAndConf(EOperationReadResult.Success));
    
    var staged = store.Contents.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), utc.Now, staged.Data)));
    ValidateResult(
        new SingleRecordReadOperationResult(EOperationReadResult.Success, "", actual.Payload),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, utc.Now),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, utc.Now, 
            EOperationReadResult.Success, EOperationAbortVote.Continue, utc.Now, utc.Now, utc.Now, "*", LastPayLoadLength:36) { LastPayLoadType = EPayloadType.Single } );
  }
  
  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = F.Runner(utc, store, repo, F.TestingListReadOperationImplementation);
    var actual = (ListRecordReadOperationResult) await runner.RunOperation(utc.Now, await CreateReadOpStateAndConf(EOperationReadResult.Success));
    
    var staged = store.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), utc.Now, s.Data))));
    ValidateResult(
        new ListRecordReadOperationResult(EOperationReadResult.Success, "", actual.PayloadList),
        actual,
        new SystemState(EOperationReadResult.Success.ToString(), EOperationReadResult.Success.ToString(), true, utc.Now),
        new ObjectState("*", "*", EOperationReadResult.Success.ToString(), true, utc.Now, 
            EOperationReadResult.Success, EOperationAbortVote.Continue, utc.Now, utc.Now, utc.Now, "*", LastPayLoadLength: staged.Count) { LastPayLoadType = EPayloadType.List } );
  }
  
  private void ValidateResult(ReadOperationResult expected, ReadOperationResult actual, SystemState expss, ObjectState expos) {
    var actualos = repo.Objects.Single().Value;
    expos = expos with { System = expss.System, Stage = expss.Stage, LastRunMessage = actualos.LastRunMessage };
    
    Assert.That(actual, Is.EqualTo(expected));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss));
    Assert.That(actualos, Is.EqualTo(expos));
  }
  
  private async Task<ReadOperationStateAndConfig> CreateReadOpStateAndConf(EOperationReadResult result) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), result.ToString()), 
        new (result.ToString(), new ("* * * * *")));
  
}