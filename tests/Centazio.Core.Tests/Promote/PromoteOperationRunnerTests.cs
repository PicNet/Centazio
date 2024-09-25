using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using Centazio.Test.Lib;
using F = Centazio.Core.Tests.TestingFactories;

namespace Centazio.Core.Tests.Promote;

public class PromoteOperationRunnerTests {

  private readonly string NAME = nameof(PromoteOperationRunnerTests);
  private readonly int RECORDS_COUNT = 100;
  
  private TestingStagedEntityStore stager;
  private TestingCtlRepository ctl;
  private TestingInMemoryCoreStorageRepository core;
  private InMemoryEntityIntraSystemMappingStore entitymap;
  private IOperationRunner<PromoteOperationConfig, PromoteOperationResult> promoter;

  [SetUp] public void SetUp() {
    (stager, ctl, core, entitymap) = (F.SeStore(), F.CtlRepo(), F.CoreRepo(), F.EntitySysMap());
    promoter = F.PromoteRunner(stager, entitymap, core);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
  
  [Test] public async Task Todo_RunOperation_will_update_staged_entities_and_core_storage() {
    await stager.Stage(NAME, NAME, Enumerable.Range(0, RECORDS_COUNT).Select(idx => idx.ToString()));
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(NAME, NAME, NAME),
        new PromoteOperationConfig(NAME, TestingDefaults.CRON_EVERY_SECOND, new EvaluateEntitiesToPromoteSuccess()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(t => true)).ToDictionary(c => c.Id);
    
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
    await stager.Stage(NAME, NAME, Enumerable.Range(0, RECORDS_COUNT).Select(idx => idx.ToString()));
    await promoter.RunOperation(new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(NAME, NAME, NAME),
        new PromoteOperationConfig(NAME, TestingDefaults.CRON_EVERY_SECOND, new EvaluateEntitiesToPromoteError()), DateTime.MinValue));
    var saved = (await core.Query<CoreEntity>(t => true)).ToDictionary(c => c.Id);
    Assert.That(saved, Is.Empty);
    
    Assert.That(stager.Contents, Has.Count.EqualTo(RECORDS_COUNT));
    
    stager.Contents.ForEach(se => {
      Assert.That(se.IgnoreReason, Is.Null);
      Assert.That(se.DatePromoted, Is.Null);
    });
    
  }
  
  private class EvaluateEntitiesToPromoteSuccess : IEvaluateEntitiesToPromote {
    public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> op, IEnumerable<StagedEntity> staged) {
      var lst = staged.ToList();
      return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(
          // todo: this cast to (ICoreEntity) is ugly, create a `record` for these types instead of named tuples
          lst.Where((_, idx) => idx % 2 == 0).Select(e => (Staged: e, Core: (ICoreEntity) new CoreEntity(e.Data, e.Data, "N", "N", new DateOnly(2000, 1, 1), UtcDate.UtcNow))).ToList(),
          lst.Where((_, idx) => idx % 2 == 1).Select(e => (Entity: e, Reason: (ValidString) $"Ignore: {e.Data}")).ToList()));
    }
    

  }

  public class EvaluateEntitiesToPromoteError : IEvaluateEntitiesToPromote {
    public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> op, IEnumerable<StagedEntity> staged) {
      return Task.FromResult((PromoteOperationResult) new ErrorPromoteOperationResult());
    }
  }
}

public class PromoteOperationRunnerHelperExtensionsTests {
  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var id = Guid.NewGuid().ToString();
    var entities = new List<CoreEntity> {
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
    var entities1 = new List<CoreEntity> {
      F.NewCoreCust("N1", "N1", "1", "c1"),
      F.NewCoreCust("N2", "N2", "2", "c2"),
      F.NewCoreCust("N3", "N3", "3", "c3"),
      F.NewCoreCust("N4", "N4", "4", "c4"),
    };
    await core.Upsert(entities1);
    
    var entities2 = new List<CoreEntity> {
      F.NewCoreCust("N12", "N12", "1", "c1"),
      F.NewCoreCust("N22", "N22", "2", "c2"),
      F.NewCoreCust("N32", "N32", "3", "c32"), // only this one gets updated as the checksum changed
      F.NewCoreCust("N42", "N42", "4", "c4"),
    };
    // ideally these methods should be strongly typed using generics 
    var uniques = await entities2.IgnoreNonMeaninfulChanges(core);
    Assert.That(uniques, Is.EquivalentTo(new [] {entities2[2]}));
  }
  
  [Test] public async Task Test_IgnoreEntitiesBouncingBack() {
    // testing this scenario: https://sequencediagram.org/index.html#initialData=C4S2BsFMAIGECUCy0C0A+aAxEA7AhjgMYh7gDOAXNAEID2ArkTNXoQNbQDKeAtgA5QAUAmTo4SKgEkcAN1ohCMABQiAjACYAzAEpohAE6Q8wSABNhSVBliQcwPAC8QtKbPmLoKpBp3Qy9gHMzYVt7J1p0GztHZ1c5BRg+fVoeWhNzKLDndGx8IhJyOPcYAHd9MBMcTzUtaAAjSEIUyDIsXE11VWhcNrziUjJtAB0cAFE7MABPRDw+PlwAr0QAGmhJH1XczfbO7UFcgn7ySNCYl16Orv88IIzT8JPo8KkAnFpDaCSUtIWLzug8K0wK08NBTPQBApjJAAHQjAAitBwMDqkz0AAtGmxfuNQMBprN5jgAtAAGbvXrLXKXIA
    // relevant steps are: 
    // Centazio->Financials: Invoice written (CRM123 becomes Fin321 in Financials)\nEntityMapping(CRM, I123, Fin, Fin321)
    var store = F.EntitySysMap();
    var core = F.NewCoreCust("N", "N", "coreid") with { SourceId = "CRM123" };
    await store.Create(EntityIntraSysMap.Create(core, Constants.System2Name).SuccessCreate("FIN321"));
    // Centazio->Centazio: Ignore promoting Fin321 as its a duplicate.\nDone by checking EntityMapping for Fin,Fin321
    var entities = new List<CoreEntity> {
      F.NewCoreCust("N", "N", "FIN1"),
      F.NewCoreCust("N", "N", "FIN2"),
      F.NewCoreCust("N", "N", "FIN3"),
      F.NewCoreCust("N", "N", "FIN321")
    };
    var filtered = await entities.IgnoreEntitiesBouncingBack(store, Constants.System2Name);
    Assert.That(filtered, Is.EquivalentTo(entities.Take(3)));
  }
}