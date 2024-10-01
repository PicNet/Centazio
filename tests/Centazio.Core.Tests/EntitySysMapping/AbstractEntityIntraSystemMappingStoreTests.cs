using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.EntitySysMapping;

// note: EntityIntraSystemMapping key is CoreEntity, CoreId, SourceSystem, SourceId, TargetSystem, TargetId
public abstract class AbstractEntityIntraSystemMappingStoreTests {

  private readonly string STR = nameof(AbstractEntityIntraSystemMappingStoreTests);
  private readonly string STR2 = nameof(CoreToExternalMap);
  
  private AbstractEntityIntraSystemMappingStore store;
  [SetUp] public void SetUp() => store = GetStore();
  [TearDown] public async Task TearDown() => await store.DisposeAsync();

  [Test] public async Task Test_upsert_single() {
    var core = TestingFactories.NewCoreCust(STR, STR);
    var original = CoreToExternalMap.Create(core, STR, Constants.CoreEntityName); 
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
    var original = new List<CoreToExternalMap.Created> { 
      CoreToExternalMap.Create(TestingFactories.NewCoreCust(STR, STR), STR, Constants.CoreEntityName).SuccessCreate(STR),
      CoreToExternalMap.Create(TestingFactories.NewCoreCust(STR2, STR2), STR2, Constants.CoreEntityName2).SuccessCreate(STR2)
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
  
  protected abstract AbstractEntityIntraSystemMappingStore GetStore();
}

public class InMemoryEntityIntraSystemMappingStoreTests : AbstractEntityIntraSystemMappingStoreTests {

  protected override AbstractEntityIntraSystemMappingStore GetStore() => new InMemoryEntityIntraSystemMappingStore();

}