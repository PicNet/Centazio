using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class CtlRepositoryDefaultTests {

  protected abstract Task<ICtlRepository> GetRepository();
  
  protected ICtlRepository repo = null!;
  
  [SetUp] public async Task SetUp() => repo = await GetRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public async Task Test_GetSystemState_returns_null_on_unknown_systems() => 
      await Assert.ThatAsync(() => repo.GetSystemState(Constants.System1Name, LifecycleStage.Defaults.Read), Is.Null);

  [Test] public async Task Test_GetSystemState_returns_correct_state_for_known_system() {
    var created = await repo.CreateSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    await Assert.ThatAsync(() => repo.GetSystemState(Constants.System1Name, LifecycleStage.Defaults.Read), Is.EqualTo(created));
  }
  
  [Test] public async Task Test_CreateSystemState_fails_for_existing_systems() {
    await repo.CreateSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateSystemState(Constants.System1Name, LifecycleStage.Defaults.Read));
  }
  
  [Test] public async Task Test_SaveSystemtState_updates_existing_state() {
    var created = await repo.CreateSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    var updated = created.SetActive(false);
    var updated2 = await repo.SaveSystemState(updated);
    var current = await repo.GetSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    Assert.That(created, Is.EqualTo(SystemState.Create(Constants.System1Name, LifecycleStage.Defaults.Read)));
    Assert.That(updated, Is.EqualTo(updated2));
    Assert.That(updated, Is.EqualTo(current));
  }
  
  [Test] public Task Test_SaveSystemState_fails_if_state_does_not_exist() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.SaveSystemState(SystemState.Create(Constants.System1Name, LifecycleStage.Defaults.Read)));
    return Task.CompletedTask;
  }
  
  [Test] public async Task Test_GetOrCreateSystemState_creates_if_not_existing() {
    var prior = await repo.GetSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    var created = await repo.GetOrCreateSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    var updated = created.SetActive(false);
    var updated2 = await repo.SaveSystemState(updated);
    var current = await repo.GetSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    
    Assert.That(prior, Is.Null);
    Assert.That(created, Is.EqualTo(SystemState.Create(Constants.System1Name, LifecycleStage.Defaults.Read)));
    Assert.That(updated, Is.EqualTo(updated2));
    Assert.That(updated, Is.EqualTo(current));
  }
  
  [Test] public async Task Test_GetObjectState_returns_null_on_unknown_systems() {
    var ss = await repo.CreateSystemState(Constants.System2Name, LifecycleStage.Defaults.Promote);
    await Assert.ThatAsync(() => repo.GetObjectState(ss, Constants.CoreEntityName), Is.Null);
  }

  [Test] public async Task Test_GetObjectState_returns_correct_state_for_known_system() {
    var ss = await repo.CreateSystemState(Constants.System1Name, LifecycleStage.Defaults.Read);
    var created = await repo.CreateObjectState(ss, Constants.CoreEntityName);
    var actual = await repo.GetObjectState(ss, Constants.CoreEntityName);
    Assert.That(actual, Is.EqualTo(created));
  }
  
  [Test] public async Task Test_CreateObjectState_fails_for_existing_object() {
    var ss = await repo.CreateSystemState(Constants.System2Name, LifecycleStage.Defaults.Promote);
    await repo.CreateObjectState(ss, Constants.CoreEntityName);
    Assert.ThrowsAsync(Is.Not.Null, () => repo.CreateObjectState(ss, Constants.CoreEntityName));
  }
  
  [Test] public async Task Test_SaveObjectState_updates_existing_state() {
    var ss = await repo.CreateSystemState(Constants.System2Name, LifecycleStage.Defaults.Promote);
    var created = await repo.CreateObjectState(ss, Constants.CoreEntityName);
    var updated = created.Success(UtcDate.UtcNow, EOperationAbortVote.Continue, LifecycleStage.Defaults.Read);
    var updated2 = await repo.SaveObjectState(updated);
    var current = await repo.GetObjectState(ss, Constants.CoreEntityName) ?? throw new Exception();
    Assert.That(created, Is.EqualTo(ObjectState.Create(Constants.System2Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName)));
    Assert.That(updated2, Is.EqualTo(updated));
    Assert.That(current, Is.EqualTo(updated2));
  }
  
  [Test] public void Test_SaveObjectState_fails_if_SystemState_does_not_exist() {
    Assert.ThrowsAsync(Is.Not.Null, () => repo.SaveObjectState(ObjectState.Create(Constants.System1Name, LifecycleStage.Defaults.Read, Constants.CoreEntityName)));
  }
  
  [Test] public async Task Test_GetOrCreateObjectState_creates_if_not_existing() {
    var ss = await repo.CreateSystemState(Constants.System2Name, LifecycleStage.Defaults.Promote);
    var prior = await repo.GetObjectState(ss, Constants.CoreEntityName);
    var created = await repo.GetOrCreateObjectState(ss, Constants.CoreEntityName);
    var updated = created.Success(UtcDate.UtcNow, EOperationAbortVote.Continue, nameof(CtlRepositoryDefaultTests));
    var updated2 = await repo.SaveObjectState(updated);
    var current = await repo.GetObjectState(ss, Constants.CoreEntityName);
    var expected = ObjectState.Create(Constants.System2Name, LifecycleStage.Defaults.Promote, Constants.CoreEntityName);
    
    Assert.That(prior, Is.Null);
    Assert.That(created, Is.EqualTo(expected));
    Assert.That(updated2, Is.EqualTo(updated));
    Assert.That(current, Is.EqualTo(updated2));
  }
}