using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Core.Tests.CoreSysMap;

public abstract class AbstractCoreToSystemMapStoreTests {

  private readonly string STR = nameof(AbstractCoreToSystemMapStoreTests);
  private readonly string STR2 = nameof(Map.CoreToSystem);
  
  private TestingInMemoryCoreStorageRepository corestore;
  private ITestingInMemoryCoreToSystemMapStore entitymap;
  
  [SetUp] public void SetUp() {
    corestore = TestingFactories.CoreRepo();
    entitymap = GetStore();
  }

  [TearDown] public async Task TearDown() {
    await corestore.DisposeAsync();
    await entitymap.DisposeAsync();
  }

  [Test] public async Task Test_upsert_single() {
    var core = TestingFactories.NewCoreCust(STR, STR);
    var original =  Map.Create(STR, core); 
    var created = (await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [original.SuccessCreate(Constants.Sys1Id1, SCS())])).Single();
    var err = created.Update().Error("Error");
    
    var list1 = await entitymap.GetAll();
    var updated = (await entitymap.Update(Constants.System1Name, Constants.CoreEntityName, [err])).Single();
    var list2 = await entitymap.GetAll();
    
    Assert.That(list1, Is.EquivalentTo(new [] { created }));
    Assert.That(updated, Is.EqualTo(err));
    Assert.That(list2, Is.EquivalentTo(new [] { err }));
  }
  
  [Test] public async Task Test_upsert_enum() {
    var original = new List<Map.Created> { 
       Map.Create(STR, TestingFactories.NewCoreCust(STR, STR)).SuccessCreate(Constants.Sys1Id1, SCS()),
       Map.Create(STR2, TestingFactories.NewCoreCust(STR2, STR2)).SuccessCreate(Constants.Sys1Id2, SCS())
    }; 
    var created = (await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, original)).ToList();
    var list1 = await entitymap.GetAll();
    var updatecmd = created.Select(e => e.Update().Error("Error")).ToList();
    var updated2 = (await entitymap.Update(Constants.System1Name, Constants.CoreEntityName, updatecmd)).ToList();
    var list2 = await entitymap.GetAll();
    var exp = created.Select(e => e.Update().Error("Error")).ToList();
        
    Assert.That(list1, Is.EquivalentTo(created));
    Assert.That(updated2, Is.EquivalentTo(exp));
    Assert.That(list2, Is.EquivalentTo(exp));
  }

  [Test] public async Task Test_duplicate_mappings_found_in_simulation() {
    List<ICoreEntity> Create(CoreEntityId coreid) => [new CoreEntity(coreid, String.Empty, String.Empty, DateOnly.MinValue, UtcDate.UtcNow)];
    var (cid_fin, cid_crm) = (new CoreEntityId("357992994"), new CoreEntityId("71c5db4e-971a-45f5-831e-643d6ca77b20"));
    var (sid_fin, sid_crm) = (new SystemEntityId("357992994"), new SystemEntityId("71c5db4e-971a-45f5-831e-643d6ca77b20"));
    // WriteOperationRunner - GetForCores Id[357992994] Type[CoreCustomer] System[CrmSystem]
    // Creating: MappingKey { CoreEntity = CoreCustomer, CoreId = 357992994, System = CrmSystem, SystemId = 71c5db4e-971a-45f5-831e-643d6ca77b20 }
    var gfc1 = await entitymap.GetNewAndExistingMappingsFromCores(Constants.System1Name, Create(cid_fin));
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, gfc1.Created.Select(c =>  c.Map.SuccessCreate(sid_crm, SCS())).ToList());
    
    // This scenario was identified in the simulation, where this GetForCores does not identify this entity as having been created before.
    // The bug here is that we promoted a new core entity because it bounced back.  However, Map.CoreToSystem should have failed gracefully and not
    // allowed a duplicate to be inserted.
    // PromoteOperationRunner - GetForCores Id[71c5db4e-971a-45f5-831e-643d6ca77b20] Type[CoreCustomer] System[CrmSystem]
    // Creating: MappingKey { CoreEntity = CoreCustomer, CoreId = 71c5db4e-971a-45f5-831e-643d6ca77b20, System = CrmSystem, SystemId = 71c5db4e-971a-45f5-831e-643d6ca77b20 }
    var gfc2 = await entitymap.GetNewAndExistingMappingsFromCores(Constants.System1Name, Create(cid_crm));
    
    var ex = Assert.ThrowsAsync<Exception>(() => entitymap.Create(Constants.System1Name, Constants.CoreEntityName, gfc2.Created.Select(c => c.Map.SuccessCreate(sid_crm, SCS())).ToList()));
    Assert.That(ex.Message, Is.EqualTo($"attempted to create duplicate CoreToSystemMaps [CRM/CoreEntity] Ids[{cid_crm.Value}]"));
  }
  
  [Test] public async Task Reproduce_duplicate_mappings_found_in_simulation() {
    var name = nameof(Reproduce_duplicate_mappings_found_in_simulation);
    async Task<CoreEntity> SimulatePromoteOperationRunner(CoreEntityId coreid, SystemName system, SystemEntityId sysid) {
      var c = new CoreEntity(coreid, name, name, DateOnly.MinValue, UtcDate.UtcNow);
      await corestore.Upsert(Constants.CoreEntityName, [new Containers.CoreChecksum(c, Helpers.TestingCoreEntityChecksum(c))]);
      await entitymap.Create(system, Constants.CoreEntityName, [ Map.Create(system, c).SuccessCreate(sysid, SCS())]);
      return c;
    }
    
    async Task<CoreEntity> SimulatePromoteOperationRunnerFixed(SystemName system, CoreEntityType coretype, List<ICoreEntity> dups) {
      var map = await entitymap.GetPreExistingSourceIdToCoreIdMap(system, coretype, dups);
      // var id = await entitymap.GetCoreIdForSystem(Constants.CoreEntityName, sysid, system) ?? throw new Exception();
      return await corestore.Get<CoreEntity>(Constants.CoreEntityName, map.Single().Value);
    }
    
    // System1 created E1
    // Centazio reads/promotes E1/C1 
    // Centazio creates map [System1:C1->E1]
    var c1 = await SimulatePromoteOperationRunner(Constants.CoreE1Id1, Constants.System1Name, Constants.Sys1Id1);
    
    // Centazio writes C1 to System2
    // Centazio creates map [System2:C1-E2]
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [ Map.Create(Constants.System2Name, c1).SuccessCreate(Constants.Sys1Id2, SCS())]);
    
    // System2 creates E2 
    // Centazio reads/promotes E2/C2
    //    - !! This is where Centazio should recognise that this entity is in fact C1 not C2
    // Centazio creates map [System2:C2-E2]
    Assert.ThrowsAsync<Exception>(() => SimulatePromoteOperationRunner(Constants.CoreE1Id2, Constants.System2Name, Constants.Sys1Id2));
    
    // Instead, the promote function should check for System2:E2 and realise that its the same core
    //    entity and ignore it if checksum matches
    var c2dup = new CoreEntity(Constants.CoreE1Id1, name, name, DateOnly.MinValue, UtcDate.UtcNow) { SourceId = Constants.Sys1Id2 };
    var c2 = await SimulatePromoteOperationRunnerFixed(Constants.System2Name, Constants.CoreEntityName, [c2dup]);
    Assert.That(Helpers.TestingCoreEntityChecksum(c1), Is.EqualTo(Helpers.TestingCoreEntityChecksum(c2))); 
  }
  
  protected abstract ITestingInMemoryCoreToSystemMapStore GetStore();
  private SystemEntityChecksum SCS() => new(Guid.NewGuid().ToString());
}

public class InMemoryCoreToSystemMapStoreTests : AbstractCoreToSystemMapStoreTests {

  protected override ITestingInMemoryCoreToSystemMapStore GetStore() => new TestingInMemoryCoreToSystemMapStore();

}

