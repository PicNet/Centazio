using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Tests.IntegrationTests;

namespace Centazio.Core.Tests.EntitySysMapping;

// note: EntityIntraSystemMapping key is CoreEntity, CoreId, SourceSystem, SourceId, TargetSystem, TargetId
public abstract class AbstractEntityIntraSystemMappingStoreTests {

  private readonly string STR = nameof(AbstractEntityIntraSystemMappingStoreTests);
  private readonly string STR2 = nameof(EntityIntraSystemMapping);
  
  private AbstractEntityIntraSystemMappingStore store;
  [SetUp] public void SetUp() => store = GetStore();
  [TearDown] public async Task TearDown() => await store.DisposeAsync();

  [Test] public async Task Test_upsert_single() {
    var original = new EntityIntraSystemMapping(STR, STR, STR, STR, STR, STR, EEntityMappingStatus.Success, UtcDate.UtcNow); 
    await store.Upsert(original);
    var list1 = await store.Get();
    var updated = original with { Status = EEntityMappingStatus.Error };
    await store.Upsert(updated);
    var list2 = await store.Get();
    var changedkey = original with { TargetSystem = STR2 };
    await store.Upsert(changedkey);
    var list3 = await store.Get();
    
    Assert.That(list1, Is.EquivalentTo(new [] { original }));
    Assert.That(list2, Is.EquivalentTo(new [] { updated }));
    Assert.That(list3, Is.EquivalentTo(new [] { updated, changedkey }));
  }
  
  [Test] public async Task Test_upsert_enum() {
    var original = new [] { 
      new EntityIntraSystemMapping(STR, STR, STR, STR, STR, STR, EEntityMappingStatus.Success, UtcDate.UtcNow),
      new EntityIntraSystemMapping(STR2, STR2, STR2, STR2, STR2, STR2, EEntityMappingStatus.Orphaned, UtcDate.UtcNow)
    }; 
    await store.Upsert(original);
    var list1 = await store.Get();
    var updated = original.Select(e => e with { Status = EEntityMappingStatus.Error }).ToList();
    await store.Upsert(updated);
    var list2 = await store.Get();
    var changedkey = updated.ToList();
    changedkey[0] = changedkey[0] with { TargetSystem = "***" }; 
    await store.Upsert(changedkey);
    var list3 = await store.Get();
    
    Assert.That(list1, Is.EquivalentTo(original));
    Assert.That(list2, Is.EquivalentTo(updated));
    Assert.That(list3, Is.EquivalentTo(updated.Concat(changedkey.Take(1))));
  }
  
  [Test] public async Task Test_FilterOutBouncedBackIds() {
    // testing this scenario: https://sequencediagram.org/index.html#initialData=C4S2BsFMAIGECUCy0C0A+aAxEA7AhjgMYh7gDOAXNAEID2ArkTNXoQNbQDKeAtgA5QAUAmTo4SKgEkcAN1ohCMABQiAjACYAzAEpohAE6Q8wSABNhSVBliQcwPAC8QtKbPmLoKpBp3Qy9gHMzYVt7J1p0GztHZ1c5BRg+fVoeWhNzKLDndGx8IhJyOPcYAHd9MBMcTzUtaAAjSEIUyDIsXE11VWhcNrziUjJtAB0cAFE7MABPRDw+PlwAr0QAGmhJH1XczfbO7UFcgn7ySNCYl16Orv88IIzT8JPo8KkAnFpDaCSUtIWLzug8K0wK08NBTPQBApjJAAHQjAAitBwMDqkz0AAtGmxfuNQMBprN5jgAtAAGbvXrLXKXIA
    // relevant steps are: 
    // Centazio->Financials: Invoice written (CRM123 becomes Fin321 in Financials)\nEntityMapping(CRM, I123, Fin, Fin321)
    await store.Upsert(new EntityIntraSystemMapping(nameof(CoreCustomer), "coreid", "CRM", "CRM123", "FIN", "FIN321", EEntityMappingStatus.Success, UtcDate.UtcNow));
    var ids = new List<string> { "FIN1", "FIN2", "FIN321", "FIN3" };
    // Centazio->Centazio: Ignore promoting Fin321 as its a duplicate.\nDone by checking EntityMapping for Fin,Fin321
    var filtered = await store.FilterOutBouncedBackIds<CoreCustomer>("FIN", ids);
    
    Assert.That(filtered, Is.EquivalentTo(ids.Where(id => id != "FIN321")));
  }

  protected abstract AbstractEntityIntraSystemMappingStore GetStore();
}

public class InMemoryEntityIntraSystemMappingStoreTests : AbstractEntityIntraSystemMappingStoreTests {

  protected override AbstractEntityIntraSystemMappingStore GetStore() => new InMemoryEntityIntraSystemMappingStore();

}