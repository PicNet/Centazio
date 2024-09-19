using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Tests.IntegrationTests;

namespace Centazio.Core.Tests.EntitySysMapping;

// note: EntityIntraSystemMapping key is CoreEntity, CoreId, SourceSystem, SourceId, TargetSystem, TargetId
public abstract class AbstractEntityIntraSystemMappingStoreTests {

  private readonly string STR = nameof(AbstractEntityIntraSystemMappingStoreTests);
  private readonly string STR2 = nameof(EntityIntraSysMap);
  
  private AbstractEntityIntraSystemMappingStore store;
  [SetUp] public void SetUp() => store = GetStore();
  [TearDown] public async Task TearDown() => await store.DisposeAsync();

  [Test] public async Task Test_upsert_single() {
    var core = TestingFactories.NewCoreCust(STR, STR);
    var original = EntityIntraSysMap.Create(core, STR); 
    var created = await store.Create(original.SuccessCreate(STR));
    var created2 = (await store.GetSingle(created.Key)).Update();
    
    var list1 = await store.GetAll();
    var updated = await store.Update(created2.Error("Error"));
    var list2 = await store.GetAll();
    
    Assert.That(list1, Is.EquivalentTo(new [] { created }));
    var exp = created2.Error("Error");
    Assert.That(updated, Is.EqualTo(exp));
    Assert.That(list2, Is.EquivalentTo(new [] { exp }));
  }
  
  [Test] public async Task Test_upsert_enum() {
    var original = new [] { 
      EntityIntraSysMap.Create(TestingFactories.NewCoreCust(STR, STR), STR).SuccessCreate(STR),
      EntityIntraSysMap.Create(TestingFactories.NewCoreCust(STR2, STR2), STR2).SuccessCreate(STR2)
    }; 
    var created = (await store.Create(original)).ToList();
    var list1 = await store.GetAll();
    var updatecmd = created.Select(e => e.Update().Error("Error")).ToList();
    var updated2 = (await store.Update(updatecmd)).ToList();
    var list2 = await store.GetAll();
    var exp = created.Select(e => e.Update().Error("Error")).ToList();
        
    Assert.That(list1, Is.EquivalentTo(created));
    Assert.That(updated2, Is.EquivalentTo(exp));
    Assert.That(list2, Is.EquivalentTo(exp));
  }
  
  [Test] public async Task Test_FilterOutBouncedBackIds() {
    // testing this scenario: https://sequencediagram.org/index.html#initialData=C4S2BsFMAIGECUCy0C0A+aAxEA7AhjgMYh7gDOAXNAEID2ArkTNXoQNbQDKeAtgA5QAUAmTo4SKgEkcAN1ohCMABQiAjACYAzAEpohAE6Q8wSABNhSVBliQcwPAC8QtKbPmLoKpBp3Qy9gHMzYVt7J1p0GztHZ1c5BRg+fVoeWhNzKLDndGx8IhJyOPcYAHd9MBMcTzUtaAAjSEIUyDIsXE11VWhcNrziUjJtAB0cAFE7MABPRDw+PlwAr0QAGmhJH1XczfbO7UFcgn7ySNCYl16Orv88IIzT8JPo8KkAnFpDaCSUtIWLzug8K0wK08NBTPQBApjJAAHQjAAitBwMDqkz0AAtGmxfuNQMBprN5jgAtAAGbvXrLXKXIA
    // relevant steps are: 
    // Centazio->Financials: Invoice written (CRM123 becomes Fin321 in Financials)\nEntityMapping(CRM, I123, Fin, Fin321)
    var core = TestingFactories.NewCoreCust("N", "N", "coreid") with { SourceId = "CRM123" };
    await store.Create(EntityIntraSysMap.Create(core, Constants.FinSystemName).SuccessCreate("FIN321"));
    
    var ids = new List<string> { "FIN1", "FIN2", "FIN321", "FIN3" };
    // Centazio->Centazio: Ignore promoting Fin321 as its a duplicate.\nDone by checking EntityMapping for Fin,Fin321
    var filtered = await store.FilterOutBouncedBackIds<CoreCustomer>(Constants.FinSystemName, ids);
    
    Assert.That(filtered, Is.EquivalentTo(ids.Where(id => id != "FIN321")));
  }

  protected abstract AbstractEntityIntraSystemMappingStore GetStore();
}

public class InMemoryEntityIntraSystemMappingStoreTests : AbstractEntityIntraSystemMappingStoreTests {

  protected override AbstractEntityIntraSystemMappingStore GetStore() => new InMemoryEntityIntraSystemMappingStore();

}