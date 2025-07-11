﻿using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseCtlRepositoryStateTests {

  protected abstract Task<ICtlRepository> GetRepository();
  
  protected ICtlRepository repo = null!;
  
  [SetUp] public async Task SetUp() => repo = await GetRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public async Task Test_GetSystemState_returns_null_on_unknown_systems() => 
      await Assert.ThatAsync(() => repo.GetSystemState(C.System1Name, LifecycleStage.Defaults.Read), Is.Null);

  [Test] public async Task Test_GetSystemState_returns_correct_state_for_known_system() {
    var created = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    await Assert.ThatAsync(() => repo.GetSystemState(C.System1Name, LifecycleStage.Defaults.Read), Is.EqualTo(created));
  }
  
  [Test] public async Task Test_CreateSystemState_fails_for_existing_systems() {
    await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read));
  }
  
  [Test] public async Task Test_SaveSystemtState_updates_existing_state() {
    var created = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var updated = created.SetActive(false);
    var updated2 = await repo.SaveSystemState(updated);
    var current = await repo.GetSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    Assert.That(created, Is.EqualTo(SystemState.Create(C.System1Name, LifecycleStage.Defaults.Read)));
    Assert.That(updated, Is.EqualTo(updated2));
    Assert.That(updated, Is.EqualTo(current));
  }
  
  [Test] public Task Test_SaveSystemState_fails_if_state_does_not_exist() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.SaveSystemState(SystemState.Create(C.System1Name, LifecycleStage.Defaults.Read)));
    return Task.CompletedTask;
  }
  
  [Test] public async Task Test_GetOrCreateSystemState_creates_if_not_existing() {
    var prior = await repo.GetSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var created = await repo.GetOrCreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var updated = created.SetActive(false);
    var updated2 = await repo.SaveSystemState(updated);
    var current = await repo.GetSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    
    Assert.That(prior, Is.Null);
    Assert.That(created, Is.EqualTo(SystemState.Create(C.System1Name, LifecycleStage.Defaults.Read)));
    Assert.That(updated, Is.EqualTo(updated2));
    Assert.That(updated, Is.EqualTo(current));
  }
  
  [Test] public async Task Test_GetObjectState_returns_null_on_unknown_systems() {
    var ss = await repo.CreateSystemState(C.System2Name, LifecycleStage.Defaults.Promote);
    await Assert.ThatAsync(() => repo.GetObjectState(ss, C.CoreEntityName), Is.Null);
  }

  [Test] public async Task Test_GetObjectState_returns_correct_state_for_known_system() {
    var ss = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var created = await repo.CreateObjectState(ss, C.CoreEntityName, UtcDate.UtcToday);
    var actual = await repo.GetObjectState(ss, C.CoreEntityName);
    Assert.That(actual, Is.EqualTo(created));
  }
  
  [Test] public async Task Test_CreateObjectState_fails_for_existing_object() {
    var ss = await repo.CreateSystemState(C.System2Name, LifecycleStage.Defaults.Promote);
    await repo.CreateObjectState(ss, C.CoreEntityName, UtcDate.UtcToday);
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateObjectState(ss, C.CoreEntityName, UtcDate.UtcToday));
  }
  
  [Test] public async Task Test_SaveObjectState_updates_existing_state() {
    var ss = await repo.CreateSystemState(C.System2Name, LifecycleStage.Defaults.Promote);
    var created = await repo.CreateObjectState(ss, C.CoreEntityName, UtcDate.UtcToday);
    var updated = created.Success(UtcDate.UtcNow, UtcDate.UtcNow, EOperationAbortVote.Continue, LifecycleStage.Defaults.Read);
    var updated2 = await repo.SaveObjectState(updated);
    var current = await repo.GetObjectState(ss, C.CoreEntityName) ?? throw new Exception();
    Assert.That(created, Is.EqualTo(ObjectState.Create(C.System2Name, LifecycleStage.Defaults.Promote, C.CoreEntityName, UtcDate.UtcToday)));
    Assert.That(updated2, Is.EqualTo(updated));
    Assert.That(current, Is.EqualTo(updated2));
  }
  
  [Test] public void Test_SaveObjectState_fails_if_SystemState_does_not_exist() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.SaveObjectState(ObjectState.Create(C.System1Name, LifecycleStage.Defaults.Read, C.CoreEntityName, UtcDate.UtcToday)));
  }
  
  [Test] public async Task Test_GetOrCreateObjectState_creates_if_not_existing() {
    var ss = await repo.CreateSystemState(C.System2Name, LifecycleStage.Defaults.Promote);
    var prior = await repo.GetObjectState(ss, C.CoreEntityName);
    var created = await repo.GetOrCreateObjectState(ss, C.CoreEntityName, UtcDate.UtcToday);
    var updated = created.Success(UtcDate.UtcNow, UtcDate.UtcNow, EOperationAbortVote.Continue, nameof(BaseCtlRepositoryStateTests));
    var updated2 = await repo.SaveObjectState(updated);
    var current = await repo.GetObjectState(ss, C.CoreEntityName);
    var expected = ObjectState.Create(C.System2Name, LifecycleStage.Defaults.Promote, C.CoreEntityName, UtcDate.UtcToday);
    
    Assert.That(prior, Is.Null);
    Assert.That(created, Is.EqualTo(expected));
    Assert.That(updated2, Is.EqualTo(updated));
    Assert.That(current, Is.EqualTo(updated2));
  }
  
  [Test] public async Task Test_EntityChanges_with_CoreEntity_Queries() {
    var start = UtcDate.UtcNow;
    await repo.SaveEntityChanges([
      EntityChange.Create(C.CoreEntityName, C.CoreE1Id1, C.System1Name, C.SystemEntityName, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName2, C.CoreE1Id1, C.System1Name, C.SystemEntityName2, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName, C.CoreE1Id2, C.System2Name, C.SystemEntityName, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName2, C.CoreE1Id2, C.System2Name, C.SystemEntityName2, C.Sys1Id1, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName, C.CoreE1Id1, C.System2Name, C.SystemEntityName, C.Sys1Id1, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
    ]);
    
    Assert.That(await repo.GetEntityChanges(C.CoreEntityName, start), Has.Count.EqualTo(3));
    Assert.That(await repo.GetEntityChanges(C.CoreEntityName2, start), Has.Count.EqualTo(2));
    Assert.That(await repo.GetEntityChanges(C.CoreEntityName, start.AddSeconds(2)), Has.Count.EqualTo(2));
    Assert.That(await repo.GetEntityChanges(C.CoreEntityName2, start.AddSeconds(2)), Has.Count.EqualTo(1));
  }
  
  [Test] public async Task Test_EntityChanges_with_SystemEntity_Queries() {
    var start = UtcDate.UtcNow;
    await repo.SaveEntityChanges([
      EntityChange.Create(C.CoreEntityName, C.CoreE1Id1, C.System1Name, C.SystemEntityName, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName2, C.CoreE1Id1, C.System1Name, C.SystemEntityName2, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName, C.CoreE1Id2, C.System2Name, C.SystemEntityName, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName2, C.CoreE1Id2, C.System2Name, C.SystemEntityName2, C.Sys1Id1, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
      EntityChange.Create(C.CoreEntityName, C.CoreE1Id1, C.System2Name, C.SystemEntityName, C.Sys1Id1, null, new EmptyCoreEntity(TestingUtcDate.DoTick())),
    ]);
    
    Assert.That(await repo.GetEntityChanges(C.System1Name, C.SystemEntityName, start), Has.Count.EqualTo(1));
    Assert.That(await repo.GetEntityChanges(C.System2Name, C.SystemEntityName, start), Has.Count.EqualTo(2));
    Assert.That(await repo.GetEntityChanges(C.System1Name, C.SystemEntityName2, start), Has.Count.EqualTo(1));
    Assert.That(await repo.GetEntityChanges(C.System2Name, C.SystemEntityName2, start), Has.Count.EqualTo(1));
    Assert.That(await repo.GetEntityChanges(C.System1Name, C.SystemEntityName, start.AddSeconds(2)), Has.Count.EqualTo(0));
    Assert.That(await repo.GetEntityChanges(C.System2Name, C.SystemEntityName, start.AddSeconds(2)), Has.Count.EqualTo(2));
    Assert.That(await repo.GetEntityChanges(C.System1Name, C.SystemEntityName2, start.AddSeconds(2)), Has.Count.EqualTo(0));
    Assert.That(await repo.GetEntityChanges(C.System2Name, C.SystemEntityName2, start.AddSeconds(2)), Has.Count.EqualTo(1));
  }
  
  [Test] public async Task Test_Transactions_With_EntityChanges() {
    if (GetType().Name == "InMemoryBaseCtlRepositoryTests") return;
    var start = UtcDate.UtcNow;
    
    // implicit transaction
    var initial = await GetList();
    await Add1(); 
    var test1 = await GetList();
    
    // commit transaction
    await using (var _ = await repo.BeginTransaction()) {
      await Add1();
    }
    
    var test2 = await GetList();
    
    // rollback transaction
    await using (var trans2 = await repo.BeginTransaction()) {
      await Add1();
      await trans2.Rollback();
    }
    var test3 = await GetList();
    
    Assert.That(initial, Has.Count.EqualTo(0));
    Assert.That(test1, Has.Count.EqualTo(1));
    Assert.That(test2, Has.Count.EqualTo(2));
    Assert.That(test3, Has.Count.EqualTo(2));
    
    Task Add1() => repo.SaveEntityChanges([EntityChange.Create(C.CoreEntityName, C.CoreE1Id1, C.System1Name, C.SystemEntityName, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick()))]);
    Task<List<EntityChange>> GetList() => repo.GetEntityChanges(C.CoreEntityName, start);
  }
  
  [Test] public async Task Test_transactions_with_multiple_methods() {
    if (GetType().Name == "InMemoryBaseCtlRepositoryTests") return;
    
    var created1 = new Map.Created(new Map.PendingCreate(new CoreEntity(new("1"), "n1", "n1", DateOnly.MaxValue), C.System1Name), new("1"), new(Guid.NewGuid().ToString()));
    var created2 = new Map.Created(new Map.PendingCreate(new CoreEntity(new("2"), "n2", "n2", DateOnly.MaxValue), C.System1Name), new("2"), new(Guid.NewGuid().ToString()));
    var result1 = (await repo.CreateSysMap(C.System1Name, C.CoreEntityName, [created1])).Single();
    
    await using var trans = await repo.BeginTransaction();
    var result2 = (await repo.CreateSysMap(C.System1Name, C.CoreEntityName, [created2])).Single();
    var updated1 = (await repo.UpdateSysMap(C.System1Name, C.CoreEntityName, [new Map.Updated(result1)])).Single();
    await repo.SaveEntityChanges([EntityChange.Create(C.CoreEntityName, C.CoreE1Id1, C.System1Name, C.SystemEntityName, C.Sys1Id2, null, new EmptyCoreEntity(TestingUtcDate.DoTick()))]);
    
    
    var all = await repo.GetMapsFromSystemIds(C.System1Name, C.CoreEntityName, [new("1"), new("2")]);
    
    Assert.That(result1.CoreId, Is.EqualTo(created1.CoreId));
    Assert.That(updated1.CoreId, Is.EqualTo(created1.CoreId));
    Assert.That(result2.CoreId, Is.EqualTo(created2.CoreId));
    Assert.That(all, Has.Count.EqualTo(2));
  }
  
  // ReSharper disable once NotAccessedPositionalProperty.Local
  record EmptyCoreEntity(DateTime LastUpdated) : ICoreEntity {

    public string DisplayName { get; } = nameof(EmptyCoreEntity);
    public object GetChecksumSubset() => String.Empty;
    public CoreEntityId CoreId { get; set; } = new(nameof(EmptyCoreEntity));

  }
}