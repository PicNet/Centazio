﻿using Centazio.Core.Checksum;
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
        new PromoteOperationConfig(typeof(System1Entity), Constants.SystemEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new EvaluateEntitiesToPromoteSuccess()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(Constants.CoreEntityName, t => true)).ToDictionary(c => c.FirstName);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    stager.Contents.ForEach((se, idx) => {
      if (idx % 2 == 0) {
        Assert.That(se.DatePromoted, Is.EqualTo(UtcDate.UtcNow));
        Assert.That(se.IgnoreReason, Is.Null);
        Assert.That(saved.ContainsKey(idx.ToString()), Is.True);
      } else {
        Assert.That(se.DatePromoted, Is.Null);
        Assert.That(se.IgnoreReason, Is.EqualTo($"Ignore: {se.Data}"));
        Assert.That(saved.ContainsKey(idx.ToString()), Is.False);
      }
    });
  }
  
  [Test] public async Task Todo_RunOperation_will_not_do_anything_on_error() {
    var ses = Enumerable.Range(0, RECORDS_COUNT).Select(idx => new System1Entity(Guid.NewGuid(), idx.ToString(), idx.ToString(), new DateOnly(2000, 1, 1), UtcDate.UtcNow)).ToList();
    await stager.Stage(Constants.System1Name, Constants.SystemEntityName, ses.Select(Json.Serialize).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(Constants.System1Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName),
        new BaseFunctionConfig(),
        new PromoteOperationConfig(typeof(System1Entity), Constants.SystemEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new EvaluateEntitiesToPromoteError()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(Constants.CoreEntityName, t => true)).ToDictionary(c => c.CoreId);
    Assert.That(saved, Is.Empty);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    
    stager.Contents.ForEach(se => {
      Assert.That(se.IgnoreReason, Is.Null);
      Assert.That(se.DatePromoted, Is.Null);
    });
    
  }
  
  private class EvaluateEntitiesToPromoteSuccess : IEvaluateEntitiesToPromote {
    
    public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
      var results = toeval.Select((eval, idx) => idx % 2 == 1 ? eval.MarkForIgnore($"Ignore: {idx}") : eval.MarkForPromotion(eval.SysEnt.To<System1Entity>().ToCoreEntity())).ToList();
      return Task.FromResult(results);
    }
  }

  public class EvaluateEntitiesToPromoteError : IEvaluateEntitiesToPromote {
    public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
      return Task.FromResult(new List<EntityEvaluationResult>());
    }
  }
}

public class PromoteOperationRunnerHelperExtensionsTests {
  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var id = Constants.CoreE1Id1;
    var entities = new List<Containers.StagedSysCore> {
      new(null!, null!, F.NewCoreCust("N1", "N1", id), true),
      new(null!, null!, F.NewCoreCust("N2", "N2", id), true),
      new(null!, null!, F.NewCoreCust("N3", "N3", id), true),
      new(null!, null!, F.NewCoreCust("N4", "N4"), true)
    };
    
    // var uniques = PromoteOperationRunner.IgnoreMultipleUpdatesToSameEntity(entities);
    // Assert.That(uniques, Is.EquivalentTo(new [] {entities[0], entities[3]}));
    Assert.Fail("todo: imlpement");
  }
  
  [Test] public async Task Test_IgnoreNonMeaninfulChanges() {
    var core = F.CoreRepo();
    var entities1 = new List<(ICoreEntity, CoreEntityChecksum)> {
      CCS(F.NewCoreCust("N1", "N1", new("1"))),
      CCS(F.NewCoreCust("N2", "N2", new("2"))),
      CCS(F.NewCoreCust("N3", "N3", new("3"))),
      CCS(F.NewCoreCust("N4", "N4", new("4")))
    };
    await core.Upsert(Constants.CoreEntityName, entities1);
    
    var entities2 = new List<Containers.StagedSysCore> {
      new (null!, null!, F.NewCoreCust("N1", "N1", new("1")), true),
      new (null!, null!, F.NewCoreCust("N2", "N2", new("2")), true),
      new (null!, null!, F.NewCoreCust("N32", "N32",  new("3")), true), // only this one gets updated as the checksum changed
      new (null!, null!, F.NewCoreCust("N4", "N4", new("4")), true)
    };
    // ideally these methods should be strongly typed using generics
    // var uniques = await PromoteOperationRunner.IgnoreNonMeaninfulChanges(entities2, Constants.CoreEntityName, core, Helpers.TestingCoreEntityChecksum);
    // Assert.That(uniques, Is.EquivalentTo(new [] {entities2[2]}));
    Assert.Fail("todo: imlpement");
    
    (ICoreEntity, CoreEntityChecksum) CCS(ICoreEntity e) => new (e, Helpers.TestingCoreEntityChecksum(e));
  }
}