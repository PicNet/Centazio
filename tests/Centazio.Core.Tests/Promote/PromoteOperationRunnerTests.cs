using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;
using Centazio.Test.Lib.CoreStorage;
using F = Centazio.Test.Lib.TestingFactories;

namespace Centazio.Core.Tests.Promote;

public class PromoteOperationRunnerTests {

  private readonly int RECORDS_COUNT = 100;
  
  private TestingStagedEntityStore stager;
  private TestingCtlRepository ctl;
  private TestingInMemoryCoreStorageRepository core;
  private InMemoryCoreToSystemMapStore entitymap;
  private IOperationRunner<PromoteOperationConfig, PromoteOperationResult> promoter;

  [SetUp] public void SetUp() {
    (stager, ctl, core, entitymap) = (F.SeStore(), F.CtlRepo(), F.CoreRepo(), F.CoreSystemMap());
    promoter = F.PromoteRunner(stager, entitymap, core);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
  
  [Test] public async Task Todo_RunOperation_will_update_staged_entities_and_core_storage() {
    var ses = Enumerable.Range(0, RECORDS_COUNT).Select(idx => new System1Entity(Guid.NewGuid(), idx.ToString(), idx.ToString(), new DateOnly(2000, 1, 1), UtcDate.UtcNow)).ToList();
    await stager.Stage(Constants.System1Name, Constants.SystemEntityName, ses.Select(Json.Serialize).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(Constants.System1Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName),
        new BaseFunctionConfig(),
        new PromoteOperationConfig(typeof(System1Entity), Constants.SystemEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new SuccessPromoteEvaluator()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(Constants.CoreEntityName, t => true)).ToDictionary(c => c.FirstName);
    
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
    await stager.Stage(Constants.System1Name, Constants.SystemEntityName, ses.Select(Json.Serialize).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(Constants.System1Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName),
        new BaseFunctionConfig { ThrowExceptions = false },
        new PromoteOperationConfig(typeof(System1Entity), Constants.SystemEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new ErrorPromoteEvaluator()), DateTime.MinValue));
    
    var saved = (await core.Query<CoreEntity>(Constants.CoreEntityName, t => true)).ToDictionary(c => c.CoreId);
    Assert.That(saved, Is.Empty);
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    stager.Contents.ForEach(se => {
      Assert.That(se.IgnoreReason, Is.Null);
      Assert.That(se.DatePromoted, Is.Null);
    });
    
  }
  
  private class SuccessPromoteEvaluator : IEvaluateEntitiesToPromote {
    
    public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
      var results = toeval.Select((eval, idx) => idx % 2 == 1 ? eval.MarkForIgnore($"Ignore: {idx}") : eval.MarkForPromotion(eval.SysEnt.To<System1Entity>().ToCoreEntity())).ToList();
      return Task.FromResult(results);
    }
  }

  public class ErrorPromoteEvaluator : IEvaluateEntitiesToPromote {
    public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
      throw new Exception(nameof(ErrorPromoteEvaluator));
    }
  }
}