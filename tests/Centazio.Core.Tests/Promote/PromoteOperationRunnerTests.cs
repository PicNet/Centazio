﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
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
  private IOperationRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult> promoter;

  [SetUp] public void SetUp() {
    (stager, ctl, core, entitymap) = (F.SeStore(), F.CtlRepo(), F.CoreRepo(), F.CoreSysMap());
    promoter = F.PromoteRunner(stager, entitymap, core);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
  
  [Test] public async Task Todo_RunOperation_will_update_staged_entities_and_core_storage() {
    await stager.Stage(Constants.System1Name, Constants.ExternalEntityName, Enumerable.Range(0, RECORDS_COUNT).Select(idx => idx.ToString()).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig, CoreEntityType>(
        ObjectState<CoreEntityType>.Create(Constants.System1Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName),
        new PromoteOperationConfig(Constants.ExternalEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new EvaluateEntitiesToPromoteSuccess()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(Constants.CoreEntityName, t => true)).ToDictionary(c => c.Id);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    stager.Contents.ForEach((se, idx) => {
      if (idx % 2 == 0) {
        Assert.That(se.DatePromoted, Is.EqualTo(UtcDate.UtcNow));
        Assert.That(se.IgnoreReason, Is.Null);
        Assert.That(saved.ContainsKey(se.Data), Is.True);
      } else {
        Assert.That(se.DatePromoted, Is.Null);
        Assert.That(se.IgnoreReason, Is.EqualTo($"Ignore: {se.Data}"));
        Assert.That(saved.ContainsKey(se.Data), Is.False);
      }
    });
  }
  
  [Test] public async Task Todo_RunOperation_will_not_do_anything_on_error() {
    await stager.Stage(Constants.System1Name, Constants.ExternalEntityName, Enumerable.Range(0, RECORDS_COUNT).Select(idx => idx.ToString()).ToList());
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig, CoreEntityType>(
        ObjectState<CoreEntityType>.Create(Constants.System1Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName),
        new PromoteOperationConfig(Constants.ExternalEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new EvaluateEntitiesToPromoteError()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(Constants.CoreEntityName, t => true)).ToDictionary(c => c.Id);
    Assert.That(saved, Is.Empty);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    
    stager.Contents.ForEach(se => {
      Assert.That(se.IgnoreReason, Is.Null);
      Assert.That(se.DatePromoted, Is.Null);
    });
    
  }
  
  private class EvaluateEntitiesToPromoteSuccess : IEvaluateEntitiesToPromote {
    public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op, List<StagedEntity> staged) {
      return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(
          staged.Where((_, idx) => idx % 2 == 0).Select(e => new StagedAndCoreEntity(e, new CoreEntity(e.Data, "N", "N", new DateOnly(2000, 1, 1), UtcDate.UtcNow))).ToList(),
          staged.Where((_, idx) => idx % 2 == 1).Select(e => new StagedEntityAndIgnoreReason(e, Reason: $"Ignore: {e.Data}")).ToList()));
    }
    

  }

  public class EvaluateEntitiesToPromoteError : IEvaluateEntitiesToPromote {
    public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> op, List<StagedEntity> staged) {
      return Task.FromResult((PromoteOperationResult) new ErrorPromoteOperationResult());
    }
  }
}

public class PromoteOperationRunnerHelperExtensionsTests {
  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var id = Guid.NewGuid().ToString();
    var entities = new List<ICoreEntity> {
      F.NewCoreCust("N1", "N1", id),
      F.NewCoreCust("N2", "N2", id),
      F.NewCoreCust("N3", "N3", id),
      F.NewCoreCust("N4", "N4"),
    };
    
    var uniques = entities.IgnoreMultipleUpdatesToSameEntity();
    Assert.That(uniques, Is.EquivalentTo(new [] {entities[0], entities[3]}));
  }
  
  [Test] public async Task Test_IgnoreNonMeaninfulChanges() {
    var core = F.CoreRepo();
    var entities1 = new List<CoreEntityAndChecksum> {
      new (F.NewCoreCust("N1", "N1", "1"), Helpers.TestingChecksum),
      new (F.NewCoreCust("N2", "N2", "2"), Helpers.TestingChecksum),
      new (F.NewCoreCust("N3", "N3", "3"), Helpers.TestingChecksum),
      new (F.NewCoreCust("N4", "N4", "4"), Helpers.TestingChecksum)
    };
    await core.Upsert(Constants.CoreEntityName, entities1);
    
    var entities2 = new List<ICoreEntity> {
      // F.NewCoreCust("N12", "N12", "1", "c1"),
      // F.NewCoreCust("N22", "N22", "2", "c2"),
      // F.NewCoreCust("N32", "N32", "3", "c32"), // only this one gets updated as the checksum changed
      // F.NewCoreCust("N42", "N42", "4", "c4"),
      
      F.NewCoreCust("N1", "N1", "1"),
      F.NewCoreCust("N2", "N2", "2"),
      F.NewCoreCust("N32", "N32", "3"), // only this one gets updated as the checksum changed
      F.NewCoreCust("N4", "N4", "4")
    };
    // ideally these methods should be strongly typed using generics
    var uniques = await entities2.IgnoreNonMeaninfulChanges(Constants.CoreEntityName, core, Helpers.TestingChecksum);
    Assert.That(uniques, Is.EquivalentTo(new [] {entities2[2]}));
  }
}