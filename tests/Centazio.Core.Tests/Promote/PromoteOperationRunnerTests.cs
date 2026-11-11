using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Promote;

public class PromoteOperationRunnerTests {

  private readonly int RECORDS_COUNT = 100;
  
  private FakeTimeProvider clock;
  private TestingStagedEntityRepository stager;
  private TestingInMemoryBaseCtlRepository ctl;
  private TestingInMemoryCoreStorageRepository core;
  private PromoteFunction promoter;

  [SetUp] public void SetUp() {
    clock = F.Clock();
    (stager, ctl, core) = (F.SeRepo(clock), F.CtlRepo(clock), F.CoreRepo());
    promoter = F.PromoteFunc(clock, stager, ctl, core);
  }
  
  [TearDown] public async Task TearDown() {
    promoter.Dispose();
    await stager.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
  
  [Test] public void Test_PromoteOperationConfig_validates_SystemEntityType_as_expected() {
    Assert.Throws<Exception>(() => { _ = new PromoteOperationConfig(C.System1Name, GetType(), new(nameof(PromoteOperationRunnerTests)), new(nameof(PromoteOperationRunnerTests)), CronExpressionsHelper.EverySecond(), (_, _) => null!); });
    
    var validtype = typeof(System1Entity);
    var validconf = new PromoteOperationConfig(C.System1Name, validtype, new(nameof(PromoteOperationRunnerTests)), new(nameof(PromoteOperationRunnerTests)), CronExpressionsHelper.EverySecond(), (_, _) => null!);
    Assert.That(validconf.SystemEntityType, Is.EqualTo(validtype));
    
    
  }
  
  [Test] public async Task Todo_RunOperation_will_update_staged_entities_and_core_storage() {
    var ses = Enumerable.Range(0, RECORDS_COUNT).Select(idx => new System1Entity(Guid.NewGuid(), idx.ToString(), idx.ToString(), new DateOnly(2000, 1, 1), clock.UtcNow())).ToList();
    await stager.Stage(C.System1Name, C.SystemEntityName, ses.Select(Json.Serialize).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(C.System1Name, LifecycleStage.Defaults.Promote, C.CoreEntityName, clock.UtcNow(), clock.UtcNow()),
        F.EmptyFunctionConfig(),
        new PromoteOperationConfig(C.System1Name, typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, SuccessfulConversionToCore), DateTime.MinValue));
    var saved = (await core.GetAllCoreEntities()).ToDictionary(c => c.FirstName);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    stager.Contents.ForEach((se, idx) => {
      if (idx % 2 == 0) {
        Assert.That(se.DatePromoted, Is.EqualTo(clock.UtcNow()));
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
    var ses = Enumerable.Range(0, RECORDS_COUNT).Select(idx => new System1Entity(Guid.NewGuid(), idx.ToString(), idx.ToString(), new DateOnly(2000, 1, 1), clock.UtcNow())).ToList();
    await stager.Stage(C.System1Name, C.SystemEntityName, ses.Select(Json.Serialize).ToList());
    try {
      await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
          ObjectState.Create(C.System1Name, LifecycleStage.Defaults.Promote, C.CoreEntityName, clock.UtcNow(), clock.UtcNow()),
          F.EmptyFunctionConfig() with { ThrowExceptions = false },
          new PromoteOperationConfig(C.System1Name, typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, ErrorConvertingToCore), DateTime.MinValue));
      Assert.Fail();
    } catch {
      var saved = (await core.GetAllCoreEntities()).ToDictionary(c => c.CoreId);
      Assert.That(saved, Is.Empty);
      Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
      stager.Contents.ForEach(se => {
        Assert.That(se.IgnoreReason, Is.Null);
        Assert.That(se.DatePromoted, Is.Null);
      });
    }
    
  }
  
  internal static Task<List<EntityEvaluationResult>> SuccessfulConversionToCore(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    // ignores by the entity, not by its position in `toeval`, as the promotion order is not the staging order
    var results = toeval.Select(eval => {
      var sysent = eval.SystemEntity.To<System1Entity>();
      var idx = Int32.Parse(sysent.FirstName);
      return idx % 2 == 1 ? eval.MarkForIgnore(new($"Ignore: {idx}")) : eval.MarkForPromotion(sysent.ToCoreEntity());
    }).ToList();
    return Task.FromResult(results);
  }
  
  internal static Task<List<EntityEvaluationResult>> ErrorConvertingToCore(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    throw new Exception(nameof(ErrorConvertingToCore));
  }
}