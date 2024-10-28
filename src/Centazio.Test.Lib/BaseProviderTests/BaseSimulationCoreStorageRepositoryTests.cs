using Centazio.Core;
using Centazio.Test.Lib.E2E;
using Centazio.Test.Lib.InMemRepos;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseSimulationCoreStorageRepositoryTests {

  private static readonly SystemName SYS = SimulationConstants.CRM_SYSTEM;
  private static readonly CoreEntityTypeName CORETYPE = CoreEntityTypeName.From<CoreMembershipType>();
  
  private InMemoryEpochTracker tracker = null!;
  private AbstractCoreStorageRepository repo = null!;
  
  protected abstract Task<AbstractCoreStorageRepository> GetRepository(InMemoryEpochTracker tracker);
  
  [SetUp] public async Task SetUp() {
    tracker = new InMemoryEpochTracker();
    repo = await GetRepository(tracker);
  }

  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public async Task Test_adding_new_entity() {
    var adding = CreateMemType();
    var added = await repo.Upsert(CORETYPE, [(adding, Helpers.TestingCoreEntityChecksum(adding))]);
    var single = await repo.GetMembershipType(adding.CoreId);
    var queried1 = await repo.GetMembershipTypes(m => true);
    var queried2 = await repo.GetExistingEntities(CORETYPE, [adding.CoreId]);
    var queried3 = await repo.GetEntitiesToWrite(new("ignore"), CORETYPE, UtcDate.UtcNow.AddSeconds(-1));
    var queried4 = await repo.GetEntitiesToWrite(new("ignore"), CORETYPE, UtcDate.UtcNow);
    
    Assert.That(added.Single(), Is.EqualTo(adding));
    Assert.That(single, Is.EqualTo(adding));
    Assert.That(queried1.Single(), Is.EqualTo(adding));
    Assert.That(queried2.Single(), Is.EqualTo(adding));
    Assert.That(queried3.Single(), Is.EqualTo(adding));
    Assert.That(queried4, Is.Empty);
    Assert.That(tracker.Added.Single(), Is.EqualTo(adding));
  }

  [Test] public async Task Test_updating_entity() {
    var entity = CreateMemType();
    var added = await repo.Upsert(CORETYPE, [(entity, Helpers.TestingCoreEntityChecksum(entity))]);
    TestingUtcDate.DoTick();
    
    var updating = entity with { Name = nameof(Test_updating_entity), DateUpdated = UtcDate.UtcNow };
    var updated = await repo.Upsert(CORETYPE, [(updating, Helpers.TestingCoreEntityChecksum(updating))]);
    var single = await repo.GetMembershipType(entity.CoreId);
    var queried1 = await repo.GetMembershipTypes(m => true);
    var queried2 = await repo.GetExistingEntities(CORETYPE, [updating.CoreId]);
    var queried3 = await repo.GetEntitiesToWrite(new("ignore"), CORETYPE, UtcDate.UtcNow.AddSeconds(-1));
    var queried4 = await repo.GetEntitiesToWrite(new("ignore"), CORETYPE, UtcDate.UtcNow);
    
    Assert.That(added.Single(), Is.EqualTo(entity));
    Assert.That(entity, Is.Not.EqualTo(updating));
    Assert.That(single, Is.EqualTo(updating));
    Assert.That(updated.Single(), Is.EqualTo(updating));
    Assert.That(queried1.Single(), Is.EqualTo(updating));
    Assert.That(queried2.Single(), Is.EqualTo(updating));
    Assert.That(queried3.Single(), Is.EqualTo(updating));
    Assert.That(queried4, Is.Empty);
    Assert.That(tracker.Added.Single(), Is.EqualTo(entity));
    Assert.That(tracker.Updated.Single(), Is.EqualTo(updating));
  }

  private CoreMembershipType CreateMemType() => new(new(Guid.NewGuid().ToString()), new(Guid.NewGuid().ToString()), nameof(CoreMembershipType)) {
    System = SYS,
    LastUpdateSystem = SYS,
    DateCreated = UtcDate.UtcNow,
    DateUpdated = UtcDate.UtcNow
  };

}