using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Promote;

public class PromoteOperationRunnerTests {

  private readonly int RECORDS_COUNT = 100;
  
  private TestingStagedEntityRepository stager;
  private TestingInMemoryCtlRepository ctl;
  private TestingInMemoryCoreStorageRepository core;
  private IOperationRunner<PromoteOperationConfig, PromoteOperationResult> promoter;

  [SetUp] public void SetUp() {
    (stager, ctl, core) = (F.SeRepo(), F.CtlRepo(), F.CoreRepo());
    promoter = F.PromoteRunner(stager, ctl, core);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
  
  [Test] public async Task Todo_RunOperation_will_update_staged_entities_and_core_storage() {
    var ses = Enumerable.Range(0, RECORDS_COUNT).Select(idx => new System1Entity(Guid.NewGuid(), idx.ToString(), idx.ToString(), new DateOnly(2000, 1, 1), UtcDate.UtcNow)).ToList();
    await stager.Stage(C.System1Name, C.SystemEntityName, ses.Select(Json.Serialize).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(C.System1Name, LifecycleStage.Defaults.Promote, C.CoreEntityName),
        new BaseFunctionConfig(),
        new PromoteOperationConfig(typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new SuccessPromoteEvaluator()), DateTime.MinValue));
    var saved = (await core.GetAll<CoreEntity>(C.CoreEntityName, t => true)).ToDictionary(c => c.FirstName);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    stager.Contents.ForEach((se, idx) => {
      if (idx % 2 == 0) {
        Assert.That(se.DatePromoted, Is.EqualTo(UtcDate.UtcNow));
        Assert.That(se.IgnoreReason, Is.Null);
        Assert.That(saved.ContainsKey(idx.ToString()), Is.True);
      } else {
        Assert.That(se.DatePromoted, Is.Null);
        Assert.That(se.IgnoreReason, Is.EqualTo($"Ignore: {idx}"));
        Assert.That(saved.ContainsKey(idx.ToString()), Is.False);
      }
    });
  }
  
  [Test] public async Task Todo_RunOperation_will_not_do_anything_on_error() {
    var ses = Enumerable.Range(0, RECORDS_COUNT).Select(idx => new System1Entity(Guid.NewGuid(), idx.ToString(), idx.ToString(), new DateOnly(2000, 1, 1), UtcDate.UtcNow)).ToList();
    await stager.Stage(C.System1Name, C.SystemEntityName, ses.Select(Json.Serialize).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(C.System1Name, LifecycleStage.Defaults.Promote, C.CoreEntityName),
        new BaseFunctionConfig { ThrowExceptions = false },
        new PromoteOperationConfig(typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new ErrorPromoteEvaluator()), DateTime.MinValue));
    
    var saved = (await core.GetAll<CoreEntity>(C.CoreEntityName, t => true)).ToDictionary(c => c.CoreId);
    Assert.That(saved, Is.Empty);
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    stager.Contents.ForEach(se => {
      Assert.That(se.IgnoreReason, Is.Null);
      Assert.That(se.DatePromoted, Is.Null);
    });
    
  }
  
  private class SuccessPromoteEvaluator : IEvaluateEntitiesToPromote {
    
    public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
      var results = toeval.Select((eval, idx) => idx % 2 == 1 ? eval.MarkForIgnore($"Ignore: {idx}") : eval.MarkForPromotion(eval.SystemEntity.To<System1Entity>().ToCoreEntity())).ToList();
      return Task.FromResult(results);
    }
  }

  public class ErrorPromoteEvaluator : IEvaluateEntitiesToPromote {
    public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
      throw new Exception(nameof(ErrorPromoteEvaluator));
    }
  }
}