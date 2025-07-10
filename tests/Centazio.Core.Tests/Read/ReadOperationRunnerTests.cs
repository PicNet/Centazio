using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
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
    var runner = F.ReadFunc(repository);
    try { 
      await runner.RunOperation(await F.CreateErroringOpStateAndConf(repo));
      Assert.Fail();
    } catch {
      Assert.That(repository.Contents, Is.Empty);
    } 
  }
  
  [Test] public async Task Test_empty_results_are_not_staged() {
    var runner = F.ReadFunc(repository);
    var opcfg = await CreateReadOpStateAndConf(EOperationResult.Success, _ => Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult()));
    var actual = await runner.RunOperation(opcfg);
    
    Assert.That(repository.Contents, Is.Empty);
    ValidateResult(new SystemState.Dto(nameof(EOperationResult.Success), nameof(EOperationResult.Success), true, UtcDate.UtcNow, UtcDate.UtcNow, nameof(ESystemStateStatus.Idle)), new EmptyReadOperationResult(), actual);
  }

  [Test] public async Task Test_valid_List_results_are_staged() {
    var runner = F.ReadFunc(repository);
    var actual = (ListReadOperationResult) await runner.RunOperation(await CreateReadOpStateAndConf(EOperationResult.Success, GetListResults));
    
    var staged = repository.Contents;
    Assert.That(staged, Is.EquivalentTo(
        staged.Select(s => new StagedEntity.Dto(s.Id, nameof(EOperationResult.Success), nameof(EOperationResult.Success), UtcDate.UtcNow, s.Data, C.IgnoreCorrId, s.StagedEntityChecksum).ToBase())));
    ValidateResult(new SystemState.Dto(nameof(EOperationResult.Success), nameof(EOperationResult.Success), true, UtcDate.UtcNow, UtcDate.UtcNow, nameof(ESystemStateStatus.Idle)), new ListReadOperationResult(actual.PayloadList, UtcDate.UtcNow), actual);
  }
  
  [Test] public void Test_results_cannot_be_invalid_PayloadLength() {
    Assert.Throws<ArgumentNullException>(() => _ = new ListReadOperationResult([], UtcDate.UtcNow));
    Assert.Throws<ArgumentNullException>(() => _ = new ListReadOperationResult([F.TestingJsonData(String.Empty)], UtcDate.UtcNow));
    Assert.Throws<ArgumentNullException>(() => _ = new ListReadOperationResult([F.TestingJsonData(null!)], UtcDate.UtcNow));
  }
  
  private void ValidateResult(SystemState.Dto expss, OperationResult expected, OperationResult actual) {
    Assert.That(Json.Serialize(actual), Is.EqualTo(Json.Serialize(expected)));
    Assert.That(repo.Systems.Single().Value, Is.EqualTo(expss.ToBase()));
  }
  
  private async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(EOperationResult result, GetUpdatesAfterCheckpointHandler impl) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(new(result.ToString()), new(result.ToString())), new SystemEntityTypeName(result.ToString()), UtcDate.UtcNow),
        F.EmptyFunctionConfig(),
        new (new SystemEntityTypeName(result.ToString()), TestingDefaults.CRON_EVERY_SECOND, impl), DateTime.MinValue);
  

  private Task<ReadOperationResult> GetListResults(OperationStateAndConfig<ReadOperationConfig> config) => 
      Task.FromResult<ReadOperationResult>(new ListReadOperationResult(Enumerable.Range(0, 100).Select(_ => F.TestingJsonData(Guid.NewGuid().ToString())).ToList(), UtcDate.UtcNow));

}