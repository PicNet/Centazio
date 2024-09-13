using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using F = Centazio.Core.Tests.TestingFactories;

namespace Centazio.Core.Tests.Promote;

public class PromoteOperationRunnerTests {

  private TestingStagedEntityStore staged;
  private TestingCtlRepository ctl;
  private TestingInMemoryCoreStorageRepository core;
  private InMemoryEntityIntraSystemMappingStore entitymap;
  private IOperationRunner<PromoteOperationConfig<CoreCustomer>, PromoteOperationResult<CoreCustomer>> promoter;

  [SetUp] public void SetUp() {
    (staged, ctl, core, entitymap) = (F.SeStore(), F.CtlRepo(), F.CoreRepo(), F.EntitySysMap());
    promoter = F.PromoteRunner(staged, entitymap, core);
  }
  
  [TearDown] public async Task TearDown() {
    await staged.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
  
  [Test] public void Todo_implement_tests() {
    Assert.Fail("todo: implement");
    Assert.That(promoter, Is.Not.Null);
  }
}

public class PromoteOperationRunnerHelperExtensionsTests {
  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var id = Guid.NewGuid().ToString();
    var entities = new List<CoreCustomer> {
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
    var entities1 = new List<CoreCustomer> {
      F.NewCoreCust("N1", "N1", "1", "c1"),
      F.NewCoreCust("N2", "N2", "2", "c2"),
      F.NewCoreCust("N3", "N3", "3", "c3"),
      F.NewCoreCust("N4", "N4", "4", "c4"),
    };
    await core.Upsert(entities1);
    
    var entities2 = new List<CoreCustomer> {
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
    await store.Upsert(new EntityIntraSystemMapping(nameof(CoreCustomer), "coreid", "CRM", "CRM123", "FIN", "FIN321", EEntityMappingStatus.Success, UtcDate.UtcNow));
    // Centazio->Centazio: Ignore promoting Fin321 as its a duplicate.\nDone by checking EntityMapping for Fin,Fin321
    var entities = new List<CoreCustomer> {
      F.NewCoreCust("N", "N", "FIN1"),
      F.NewCoreCust("N", "N", "FIN2"),
      F.NewCoreCust("N", "N", "FIN3"),
      F.NewCoreCust("N", "N", "FIN321")
    };
    var filtered = await entities.IgnoreEntitiesBouncingBack(store, "FIN");
    Assert.That(filtered, Is.EquivalentTo(entities.Take(3)));
  }
}