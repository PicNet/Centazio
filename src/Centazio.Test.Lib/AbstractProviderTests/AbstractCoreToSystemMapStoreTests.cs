﻿using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib.CoreStorage;
using NUnit.Framework;

namespace Centazio.Test.Lib.AbstractProviderTests;

public abstract class AbstractCoreToSystemMapStoreTests {

  private const string STR = nameof(AbstractCoreToSystemMapStoreTests);
  private const string STR2 = nameof(Map.CoreToSystemMap);
  private TestingInMemoryCoreStorageRepository corestore = null!;
  private ITestingCoreToSystemMapStore entitymap = null!;
  
  [SetUp] public async Task SetUp() {
    corestore = TestingFactories.CoreRepo();
    entitymap = await GetStore();
  }

  [TearDown] public async Task TearDown() {
    await corestore.DisposeAsync();
    await entitymap.DisposeAsync();
  }

  [Test] public async Task Test_upsert_single() {
    var core = TestingFactories.NewCoreCust(STR, STR);
    var original =  Map.Create(Constants.System1Name, core);
    TestingUtcDate.DoTick();
    var created = (await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [original.SuccessCreate(Constants.Sys1Id1, SCS())])).Single();
    var err = created.Update().Error("Error");
    
    var list1 = await entitymap.GetAll();
    
    TestingUtcDate.DoTick();
    var updated = (await entitymap.Update(Constants.System1Name, Constants.CoreEntityName, [err])).Single();
    var list2 = await entitymap.GetAll();
    
    Assert.That(Json.AreJsonEqual(list1.Single(), created));
    Assert.That(updated, Is.EqualTo(err));
    Assert.That(Json.AreJsonEqual(list2.Single(), err));
  }
  
  [Test] public async Task Test_upsert_enum() {
    var original = new List<Map.Created> { 
       Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR)).SuccessCreate(Constants.Sys1Id1, SCS()),
       Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR2, STR2)).SuccessCreate(Constants.Sys1Id2, SCS())
    }; 
    TestingUtcDate.DoTick();
    var created = (await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, original)).ToList();
    var list1 = await entitymap.GetAll();
    
    TestingUtcDate.DoTick();
    var updatecmd = created.Select(e => e.Update().Error("Error")).ToList();
    var updated2 = (await entitymap.Update(Constants.System1Name, Constants.CoreEntityName, updatecmd)).ToList();
    var list2 = await entitymap.GetAll();
    var exp = created.Select(e => e.Update().Error("Error")).ToList();
        
    Assert.That(Json.AreJsonEqual(list1, created));
    Assert.That(updated2, Is.EquivalentTo(exp));
    Assert.That(Json.AreJsonEqual(list2, exp));
  }
  
  [Test] public async Task Test_creating_unique_by_SystemId_works() {
    var map1 = Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR)).SuccessCreate(Constants.Sys1Id1, SCS());
    var map2 = Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR)).SuccessCreate(Constants.Sys1Id2, SCS());
    
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map1]);
    TestingUtcDate.DoTick();
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map2]);
  }
  
  [Test] public async Task Test_creating_duplicates_by_SystemId_throws_error() {
    var map1 = Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR)).SuccessCreate(Constants.Sys1Id1, SCS());
    var map2 = Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR)).SuccessCreate(Constants.Sys1Id1, SCS()); // same SystemId
    
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map1]);
    TestingUtcDate.DoTick();
    Assert.ThrowsAsync<Exception>(() => entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map2]));
  }
  
  [Test] public async Task Test_creating_duplicates_by_CoreId_throws_error() {
    var map1 = Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR, new(nameof(AbstractCoreToSystemMapStoreTests)))).SuccessCreate(Constants.Sys1Id1, SCS());
    var map2 = Map.Create(Constants.System1Name, TestingFactories.NewCoreCust(STR, STR, new(nameof(AbstractCoreToSystemMapStoreTests)))).SuccessCreate(Constants.Sys1Id1, SCS()); // same CoreId
    
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map1]);
    TestingUtcDate.DoTick();
    Assert.ThrowsAsync<Exception>(() => entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map2]));
  }
  
  [Test] public async Task Test_updating_with_no_missing_works() {
    var entity = TestingFactories.NewCoreCust(STR, STR);
    var map = Map.Create(Constants.System1Name, entity).SuccessCreate(Constants.Sys1Id1, SCS());
    
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map]);
    TestingUtcDate.DoTick();
    await entitymap.Update(Constants.System1Name, Constants.CoreEntityName, [map.Update().SuccessUpdate(new("newchecksum"))]);
  }
  
  [Test] public async Task Test_updating_missing_by_SystemId_throws_error() {
    var entity = TestingFactories.NewCoreCust(STR, STR);
    var map1 = Map.Create(Constants.System1Name, entity).SuccessCreate(Constants.Sys1Id1, SCS());
    var map2 = Map.Create(Constants.System1Name, entity).SuccessCreate(Constants.Sys1Id2, SCS());
    
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map1]);
    TestingUtcDate.DoTick();
    Assert.ThrowsAsync<Exception>(() => entitymap.Update(Constants.System1Name, Constants.CoreEntityName, [map2.Update().SuccessUpdate(new("newchecksum"))]));
  }
  
  [Test] public async Task Test_updating_missing_by_CoreId_throws_error() {
    var entity1 = TestingFactories.NewCoreCust(STR, STR);
    var entity2 = TestingFactories.NewCoreCust(STR, STR);
    var map1 = Map.Create(Constants.System1Name, entity1).SuccessCreate(Constants.Sys1Id1, SCS());
    var map2 = Map.Create(Constants.System1Name, entity2).SuccessCreate(Constants.Sys1Id1, SCS());
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, [map1]);
    TestingUtcDate.DoTick();
    Assert.ThrowsAsync<Exception>(() => entitymap.Update(Constants.System1Name, Constants.CoreEntityName, [map2.Update().SuccessUpdate(new("newchecksum"))]));
  }

  [Test] public async Task Test_duplicate_mappings_found_in_simulation() {
    List<ICoreEntity> Create(CoreEntityId coreid) => [new CoreEntity(coreid, String.Empty, String.Empty, DateOnly.MinValue)];
    var (cid_fin, cid_crm) = (new CoreEntityId("357992994"), new CoreEntityId("71c5db4e-971a-45f5-831e-643d6ca77b20"));
    var sid_crm = new SystemEntityId("71c5db4e-971a-45f5-831e-643d6ca77b20");
    // WriteOperationRunner - GetForCores Id[357992994] Type[CoreCustomer] System[CrmSystem]
    // Creating: MappingKey { CoreEntity = CoreCustomer, CoreId = 357992994, System = CrmSystem, SystemId = 71c5db4e-971a-45f5-831e-643d6ca77b20 }
    var gfc1 = await entitymap.GetNewAndExistingMappingsFromCores(Constants.System1Name, Constants.CoreEntityName , Create(cid_fin));
    await entitymap.Create(Constants.System1Name, Constants.CoreEntityName, gfc1.Created.Select(c =>  c.Map.SuccessCreate(sid_crm, SCS())).ToList());
    
    // This scenario was identified in the simulation, where this GetForCores does not identify this entity as having been created before.
    // The bug here is that we promoted a new core entity because it bounced back.  However, Map.CoreToSystem should have failed gracefully and not
    // allowed a duplicate to be inserted.
    // PromoteOperationRunner - GetForCores Id[71c5db4e-971a-45f5-831e-643d6ca77b20] Type[CoreCustomer] System[CrmSystem]
    // Creating: MappingKey { CoreEntity = CoreCustomer, CoreId = 71c5db4e-971a-45f5-831e-643d6ca77b20, System = CrmSystem, SystemId = 71c5db4e-971a-45f5-831e-643d6ca77b20 }
    var gfc2 = await entitymap.GetNewAndExistingMappingsFromCores(Constants.System1Name, Constants.CoreEntityName, Create(cid_crm));
    
    Assert.ThrowsAsync<Exception>(() => entitymap.Create(Constants.System1Name, Constants.CoreEntityName, gfc2.Created.Select(c => c.Map.SuccessCreate(sid_crm, SCS())).ToList()));
  }
  
  [Test] public async Task Reproduce_duplicate_mappings_found_in_simulation() {
    var name = nameof(Reproduce_duplicate_mappings_found_in_simulation);
    async Task<CoreEntity> SimulatePromoteOperationRunner(CoreEntityId coreid, SystemName system, SystemEntityId sysid) {
      TestingUtcDate.DoTick();
      var c = new CoreEntity(coreid, name, name, DateOnly.MinValue);
      await corestore.Upsert(Constants.CoreEntityName, [(c, Helpers.TestingCoreEntityChecksum(c))]);
      await entitymap.Create(system, Constants.CoreEntityName, [ Map.Create(system, c).SuccessCreate(sysid, SCS())]);
      return c;
    }
    
    async Task<CoreEntity> SimulatePromoteOperationRunnerFixed(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> dups) {
      TestingUtcDate.DoTick();
      var map = await entitymap.GetPreExistingSystemIdToCoreIdMap(system, coretype, dups);
      // var id = await entitymap.GetCoreIdForSystem(Constants.CoreEntityName, sysid, system) ?? throw new Exception();
      return (await corestore.Get(Constants.CoreEntityName, [map.Single().Value])).Cast<CoreEntity>().Single();
    }
    
    // System1 created E1
    // Centazio reads/promotes E1/C1 
    // Centazio creates map [System1:C1->E1]
    var c1 = await SimulatePromoteOperationRunner(Constants.CoreE1Id1, Constants.System1Name, Constants.Sys1Id1);
    
    // Centazio writes C1 to System2
    // Centazio creates map [System2:C1-E2]
    TestingUtcDate.DoTick();
    await entitymap.Create(Constants.System2Name, Constants.CoreEntityName, [ Map.Create(Constants.System2Name, c1).SuccessCreate(Constants.Sys1Id2, SCS())]);
    
    // System2 creates E2 
    // Centazio reads/promotes E2/C2
    //    - !! This is where Centazio should recognise that this entity is in fact C1 not C2
    // Centazio creates map [System2:C2-E2]
    Assert.ThrowsAsync<Exception>(() => SimulatePromoteOperationRunner(Constants.CoreE1Id2, Constants.System2Name, Constants.Sys1Id2));
    
    // Instead, the promote function should check for System2:E2 and realise that its the same core
    //    entity and ignore it if checksum matches
    var c2dup = new CoreEntity(Constants.CoreE1Id1, name, name, DateOnly.MinValue) { SystemId = Constants.Sys1Id2 };
    var c2 = await SimulatePromoteOperationRunnerFixed(Constants.System2Name, Constants.CoreEntityName, [c2dup]);
    Assert.That(Helpers.TestingCoreEntityChecksum(c1), Is.EqualTo(Helpers.TestingCoreEntityChecksum(c2))); 
  }
  
  protected abstract Task<ITestingCoreToSystemMapStore> GetStore();
  private SystemEntityChecksum SCS() => new(Guid.NewGuid().ToString());
}