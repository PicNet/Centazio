using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Tests.IntegrationTests;

namespace Centazio.Core.Tests.Ctl;

public abstract class CtlRepositoryDefaultTests {

  protected const string NAME = nameof(CtlRepositoryDefaultTests);
  protected const string NAME2 = nameof(CtlRepositoryDefaultTests) + "2";
  
  protected abstract Task<ICtlRepository> GetRepository();
  
  protected ICtlRepository repo;
  
  [SetUp] public async Task SetUp() => repo = await GetRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();
  
  [Test] public async Task Test_GetSystemState_returns_null_on_unknown_systems() => 
      await Assert.ThatAsync(() => repo.GetSystemState(NAME, NAME), Is.Null);

  [Test] public async Task Test_GetSystemState_returns_correct_state_for_known_system() {
    var created = await repo.CreateSystemState(NAME, NAME);
    await Assert.ThatAsync(() => repo.GetSystemState(NAME, NAME), Is.EqualTo(created));
  }
  
  [Test] public async Task Test_CreateSystemState_fails_for_existing_systems() {
    await repo.CreateSystemState(NAME, NAME);
    
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateSystemState(NAME, NAME));
  }
  
  [Test] public async Task Test_SaveSystemtState_updates_existing_state() {
    var created = await repo.CreateSystemState(NAME, NAME);
    var updated = created.SetActive(false);
    var updated2 = await repo.SaveSystemState(updated);
    var current = await repo.GetSystemState(NAME, NAME);
    Assert.That(created, Is.EqualTo(SystemState.Create(NAME, NAME)));
    Assert.That(updated, Is.EqualTo(updated2));
    Assert.That(updated, Is.EqualTo(current));
  }
  
  [Test] public Task Test_SaveSystemState_fails_if_state_does_not_exist() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.SaveSystemState(SystemState.Create(NAME, NAME)));
    return Task.CompletedTask;
  }
  
  [Test] public async Task Test_GetOrCreateSystemState_creates_if_not_existing() {
    var prior = await repo.GetSystemState(NAME, NAME);
    var created = await repo.GetOrCreateSystemState(NAME, NAME);
    var updated = created.SetActive(false);
    var updated2 = await repo.SaveSystemState(updated);
    var current = await repo.GetSystemState(NAME, NAME);
    
    Assert.That(prior, Is.Null);
    Assert.That(created, Is.EqualTo(SystemState.Create(NAME, NAME)));
    Assert.That(updated, Is.EqualTo(updated2));
    Assert.That(updated, Is.EqualTo(current));
  }
  
  [Test] public async Task Test_GetObjectState_returns_null_on_unknown_systems() {
    var ss = await repo.CreateSystemState(NAME2, NAME2);
    await Assert.ThatAsync(() => repo.GetObjectState(ss, Constants.CoreEntityName), Is.Null);
  }

  [Test] public async Task Test_GetObjectState_returns_correct_state_for_known_system() {
    var ss = await repo.CreateSystemState(NAME2, NAME2);
    var created = await repo.CreateObjectState(ss, Constants.CoreEntityName);
    var actual = await repo.GetObjectState(ss, Constants.CoreEntityName);
    Assert.That(actual, Is.EqualTo(created));
  }
  
  [Test] public void Test_CreateObjectState_fails_for_non_existing_system() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateObjectState(SystemState.Create(NAME, NAME), Constants.CoreEntityName));
  }
  
  [Test] public async Task Test_CreateObjectState_fails_for_existing_object() {
    var ss = await repo.CreateSystemState(NAME2, NAME2);
    await repo.CreateObjectState(ss, Constants.CoreEntityName);
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateObjectState(ss, Constants.CoreEntityName));
  }
  
  [Test] public async Task Test_SaveObjectState_updates_existing_state() {
    var ss = await repo.CreateSystemState(NAME2, NAME2);
    var created = await repo.CreateObjectState(ss, Constants.CoreEntityName);
    var updated = created.Success(UtcDate.UtcNow, EOperationAbortVote.Continue, NAME);
    var updated2 = await repo.SaveObjectState(updated);
    var current = await repo.GetObjectState(ss, Constants.CoreEntityName);
    Assert.That(created, Is.EqualTo(ObjectState<CoreEntityType>.Create(NAME2, NAME2, Constants.CoreEntityName)));
    Assert.That(updated2, Is.EqualTo(updated));
    Assert.That(current, Is.EqualTo(updated2));
  }
  
  [Test] public void Test_SaveObjectState_fails_if_SystemState_does_not_exist() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.SaveObjectState(ObjectState<CoreEntityType>.Create(NAME, NAME, Constants.CoreEntityName)));
  }
  
  [Test] public async Task Test_GetOrCreateObjectState_creates_if_not_existing() {
    var ss = await repo.CreateSystemState(NAME2, NAME2);
    var prior = await repo.GetObjectState(ss, Constants.CoreEntityName);
    var created = await repo.GetOrCreateObjectState(ss, Constants.CoreEntityName);
    var updated = created.Success(UtcDate.UtcNow, EOperationAbortVote.Continue, NAME);
    var updated2 = await repo.SaveObjectState(updated);
    var current = await repo.GetObjectState(ss, Constants.CoreEntityName);
    
    Assert.That(prior, Is.Null);
    var expected = ObjectState<CoreEntityType>.Create(NAME2, NAME2, Constants.CoreEntityName);
    Console.WriteLine($"CREATED[{created.Object.GetType().Name}] EXPECTED[{expected.GetType().Name}]");
    Assert.That(created, Is.EqualTo(expected));
    Assert.That(updated2, Is.EqualTo(updated));
    Assert.That(current, Is.EqualTo(updated2));
  }
}