using Centazio.Test.Lib.InMemRepos;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseCtlRepositoryMappingsTests {

  private const string FIRST_NAME = nameof(FIRST_NAME);
  private const string LAST_NAME = nameof(LAST_NAME);
  
  private TestingInMemoryCoreStorageRepository corestore = null!;
  private ITestingCtlRepository ctl = null!;
  private FakeTimeProvider clock = null!;
  private IGuidGenerator ids = null!;
  
  [SetUp] public async Task SetUp() {
    (clock, ids) = (F.Clock(), F.Ids());
    corestore = F.CoreRepo();
    ctl = await GetRepository(clock);
  }

  [TearDown] public async Task TearDown() {
    await corestore.DisposeAsync();
    await ctl.DisposeAsync();
  }

  [Test] public async Task Test_upsert_single() {
    var core = NewCore(FIRST_NAME, FIRST_NAME);
    var (start, original) = (clock.UtcNow(), Map.Create(C.System1Name, core.CoreEntity, clock.UtcNow()));
    var createdat = clock.Tick();
    var created = (await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [original.SuccessCreate(C.Sys1Id1, SCS())])).Single();
    var err = created.Update().Error("Error");
    
    var list1 = await ctl.GetAllMaps();
    
    var updatedat = clock.Tick();
    var updated = (await ctl.UpdateSysMap(C.System1Name, C.CoreEntityName, [err])).Single();
    var list2 = await ctl.GetAllMaps();
    var exp = err with { DateUpdated = updatedat, DateLastError = updatedat };
    
    Assert.That(Json.ValidateJsonEqual(list1.Single(), created));
    Assert.That((created.DateCreated, created.DateUpdated, created.DateLastSuccess), Is.EqualTo((start, createdat, createdat)));
    Assert.That(updated, Is.EqualTo(exp));
    Assert.That((updated.DateCreated, updated.DateUpdated, updated.DateLastSuccess, updated.DateLastError), Is.EqualTo((start, updatedat, createdat, updatedat)));
    Assert.That(Json.ValidateJsonEqual(list2.Single(), exp));
  }
  
  [Test] public async Task Test_successful_update_is_stamped_with_the_repository_clock() {
    var created = (await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS())])).Single();
    var updatedat = clock.Tick(TimeSpan.FromDays(1));
    var updated = (await ctl.UpdateSysMap(C.System1Name, C.CoreEntityName, [created.Update().SuccessUpdate(SCS())])).Single();
    
    Assert.That((updated.DateUpdated, updated.DateLastSuccess, updated.DateLastError), Is.EqualTo((updatedat, updatedat, (DateTime?) null)));
    Assert.That(Json.ValidateJsonEqual((await ctl.GetAllMaps()).Single(), updated));
  }
  
  [Test] public async Task Test_upsert_enum() {
    var original = new List<Map.Created> { 
      Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS()),
      Map.Create(C.System1Name, NewCore(LAST_NAME, LAST_NAME).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id2, SCS())
    }; 
    clock.Tick();
    var created = (await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, original)).ToList();
    var list1 = await ctl.GetAllMaps();
    
    var updatedat = clock.Tick();
    var updatecmd = created.Select(e => e.Update().Error("Error")).ToList();
    var updated2 = (await ctl.UpdateSysMap(C.System1Name, C.CoreEntityName, updatecmd)).ToList();
    var list2 = await ctl.GetAllMaps();
    var exp = created.Select(e => e.Update().Error("Error") with { DateUpdated = updatedat, DateLastError = updatedat }).ToList();
        
    Assert.That(Json.ValidateJsonEquivalent(list1, created));
    Assert.That(updated2, Is.EquivalentTo(exp));
    Assert.That(Json.ValidateJsonEquivalent(list2, exp));
  }
  
  [Test] public async Task Test_creating_unique_by_SystemId_works() {
    var map1 = Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS());
    var map2 = Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id2, SCS());
    
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map1]);
    clock.Tick();
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map2]);
  }
  
  [Test] public async Task Test_creating_duplicates_by_SystemId_throws_error() {
    var map1 = Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME, C.CoreE1Id1).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS());
    var map2 = Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME, C.CoreE1Id2).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS()); // same SystemId
    
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map1]);
    clock.Tick();
    await AssertException(() => ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map2]));
  }
  
  [Test] public async Task Test_creating_duplicates_by_CoreId_throws_error() {
    var map1 = Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME, C.CoreE1Id1).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS());
    var map2 = Map.Create(C.System1Name, NewCore(FIRST_NAME, FIRST_NAME, C.CoreE1Id1).CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id2, SCS()); 
    
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map1]);
    clock.Tick();
    await AssertException(() => ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map2]));
  }
  
  [Test] public async Task Test_updating_with_no_missing_works() {
    var entity = NewCore(FIRST_NAME, FIRST_NAME);
    var map = Map.Create(C.System1Name, entity.CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS());
    
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map]);
    clock.Tick();
    await ctl.UpdateSysMap(C.System1Name, C.CoreEntityName, [map.Update().SuccessUpdate(new("newchecksum"))]);
  }
  
  [Test] public async Task Test_updating_missing_by_CoreId_throws_error() {
    var entity1 = NewCore(FIRST_NAME, FIRST_NAME);
    var entity2 = NewCore(FIRST_NAME, FIRST_NAME);
    var map1 = Map.Create(C.System1Name, entity1.CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS());
    var map2 = Map.Create(C.System1Name, entity2.CoreEntity, clock.UtcNow()).SuccessCreate(C.Sys1Id1, SCS());
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, [map1]);
    clock.Tick();
    await AssertException(() => ctl.UpdateSysMap(C.System1Name, C.CoreEntityName, [map2.Update().SuccessUpdate(new("newchecksum"))]));
  }

  [Test] public async Task Test_duplicate_mappings_found_in_simulation() {
    List<ICoreEntity> Create(CoreEntityId coreid) => [new CoreEntity(coreid, String.Empty, String.Empty, DateOnly.MinValue)];
    var (cid_fin, cid_crm) = (new CoreEntityId("357992994"), new CoreEntityId("71c5db4e-971a-45f5-831e-643d6ca77b20"));
    var sid_crm = new SystemEntityId("71c5db4e-971a-45f5-831e-643d6ca77b20");
    // WriteOperationRunner - GetForCores Id[357992994] Type[CoreCustomer] System[CrmSystem]
    // Creating: MappingKey { CoreEntity = CoreCustomer, CoreId = 357992994, System = CrmSystem, SystemId = 71c5db4e-971a-45f5-831e-643d6ca77b20 }
    var gfc1 = await ctl.GetNewAndExistingMapsFromCores(C.System1Name, C.CoreEntityName , Create(cid_fin));
    await ctl.CreateSysMap(C.System1Name, C.CoreEntityName, gfc1.Created.Select(c =>  c.Map.SuccessCreate(sid_crm, SCS())).ToList());
    
    // This scenario was identified in the simulation, where this GetForCores does not identify this entity as having been created before.
    // The bug here is that we promoted a new core entity because it bounced back.  However, Map.CoreToSystem should have failed gracefully and not
    // allowed a duplicate to be inserted.
    // PromoteOperationRunner - GetForCores Id[71c5db4e-971a-45f5-831e-643d6ca77b20] Type[CoreCustomer] System[CrmSystem]
    // Creating: MappingKey { CoreEntity = CoreCustomer, CoreId = 71c5db4e-971a-45f5-831e-643d6ca77b20, System = CrmSystem, SystemId = 71c5db4e-971a-45f5-831e-643d6ca77b20 }
    var gfc2 = await ctl.GetNewAndExistingMapsFromCores(C.System1Name, C.CoreEntityName, Create(cid_crm));
    
    await AssertException(() => ctl.CreateSysMap(C.System1Name, C.CoreEntityName, gfc2.Created.Select(c => c.Map.SuccessCreate(sid_crm, SCS())).ToList()));
  }
  
  [Test] public async Task Reproduce_duplicate_mappings_found_in_simulation() {
    var name = nameof(Reproduce_duplicate_mappings_found_in_simulation);
    async Task<CoreEntity> SimulatePromoteOperationRunner(CoreEntityId coreid, SystemName system, SystemEntityTypeName systype, SystemEntityId sysid) {
      clock.Tick();
      var c = new CoreEntity(coreid, name, name, DateOnly.MinValue);
      await corestore.Upsert(C.CoreEntityName, [CoreEntityAndMeta.Create(system, systype, sysid, c, Helpers.TestingCoreEntityChecksum(c), clock.UtcNow())]);
      await ctl.CreateSysMap(system, C.CoreEntityName, [ Map.Create(system, c, clock.UtcNow()).SuccessCreate(sysid, SCS())]);
      return c;
    }
    
    async Task<CoreEntity> SimulatePromoteOperationRunnerFixed(SystemName system, CoreEntityTypeName coretype, List<CoreEntityAndMeta> dups) {
      clock.Tick();
      var map = await ctl.GetMapsFromSystemIds(system, coretype, dups.Select(e => e.Meta.OriginalSystemId).ToList());
      return (await corestore.GetExistingEntities(C.CoreEntityName, [map.Single().CoreId])).Single().As<CoreEntity>();
    }
    
    // System1 created E1
    // Centazio reads/promotes E1/C1 
    // Centazio creates map [System1:C1->E1]
    var c1 = await SimulatePromoteOperationRunner(C.CoreE1Id1, C.System1Name, C.SystemEntityName, C.Sys1Id1);
    
    // Centazio writes C1 to System2
    // Centazio creates map [System2:C1-E2]
    clock.Tick();
    await ctl.CreateSysMap(C.System2Name, C.CoreEntityName, [ Map.Create(C.System2Name, c1, clock.UtcNow()).SuccessCreate(C.Sys1Id2, SCS())]);
    
    // System2 creates E2 
    // Centazio reads/promotes E2/C2
    //    - !! This is where Centazio should recognise that this entity is in fact C1 not C2
    // Centazio creates map [System2:C2-E2]
    await AssertException(() => SimulatePromoteOperationRunner(C.CoreE1Id2, C.System2Name, C.SystemEntityName, C.Sys1Id2));
    
    // Instead, the promote function should check for System2:E2 and realise that its the same core
    //    entity and ignore it if checksum matches
    var c2cem = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, C.Sys1Id2, new CoreEntity(C.CoreE1Id1, name, name, DateOnly.MinValue), Helpers.TestingCoreEntityChecksum, clock.UtcNow());
    var c2 = await SimulatePromoteOperationRunnerFixed(C.System2Name, C.CoreEntityName, [c2cem]);
    Assert.That(Helpers.TestingCoreEntityChecksum(c1), Is.EqualTo(Helpers.TestingCoreEntityChecksum(c2))); 
  }
  
  protected abstract Task<ITestingCtlRepository> GetRepository(TimeProvider clock);
  private SystemEntityChecksum SCS() => new(ids.NewGuid().ToString());
  private CoreEntityAndMeta NewCore(string first, string last, CoreEntityId? coreid = null) => F.NewCoreEntity(first, last, clock.UtcNow(), coreid ?? new(ids.NewGuid().ToString()));
  
  private async Task AssertException(Func<Task> action) {
    try {
      await action();
      Assert.Fail("Exception expected");
    } catch { /* ignore */ }
  }
}