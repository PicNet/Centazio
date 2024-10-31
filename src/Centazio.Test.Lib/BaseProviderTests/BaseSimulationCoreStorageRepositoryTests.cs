using Centazio.Core;
using Centazio.Core.CoreRepo;
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
    var adding = CreateMemTypeCEAM();
    var added = await repo.Upsert(CORETYPE, [(adding, Helpers.TestingCoreEntityChecksum(adding.CoreEntity))]);
    var single = await repo.GetMembershipType(adding.CoreEntity.CoreId);
    var queried1 = await repo.GetMembershipTypes(m => true);
    var queried2 = await repo.GetExistingEntities(CORETYPE, [adding.CoreEntity.CoreId]);
    var queried3 = await repo.GetEntitiesToWrite(new("ignore"), CORETYPE, UtcDate.UtcNow.AddSeconds(-1));
    var queried4 = await repo.GetEntitiesToWrite(new("ignore"), CORETYPE, UtcDate.UtcNow);
    
    Assert.That(added.Single(), Is.EqualTo(adding));
    Assert.That(single, Is.EqualTo(adding.CoreEntity));
    Assert.That(queried1.Single(), Is.EqualTo(adding.CoreEntity));
    Assert.That(queried2.Single(), Is.EqualTo(adding));
    Assert.That(queried3.Single(), Is.EqualTo(adding));
    Assert.That(queried4, Is.Empty);
    Assert.That(tracker.Added.Single(), Is.EqualTo(adding.CoreEntity));
  }

  [Test] public async Task Test_updating_entity() {
    var ceam = CreateMemTypeCEAM();
    var added = await repo.Upsert(CORETYPE, [(ceam, Helpers.TestingCoreEntityChecksum(ceam.CoreEntity))]);
    TestingUtcDate.DoTick();
    
    var updating = ceam.Update((CoreMembershipType) ceam.CoreEntity with { Name = nameof(Test_updating_entity)} , SYS);
    var updated = await repo.Upsert(CORETYPE, [(updating, Helpers.TestingCoreEntityChecksum(updating.CoreEntity))]);
    var single = await repo.GetMembershipType(ceam.CoreEntity.CoreId);
    var queried1 = await repo.GetMembershipTypes(m => true);
    var queried2 = await repo.GetExistingEntities(CORETYPE, [updating.CoreEntity.CoreId]);
    var queried3 = await repo.GetEntitiesToWrite(new("ignore exclude"), CORETYPE, UtcDate.UtcNow.AddSeconds(-1));
    var queried4 = await repo.GetEntitiesToWrite(new("ignore exclude"), CORETYPE, UtcDate.UtcNow);
    
    Assert.That(added.Single(), Is.EqualTo(ceam));
    Assert.That(ceam, Is.Not.EqualTo(updating));
    Assert.That(single, Is.EqualTo(updating.CoreEntity));
    Assert.That(updated.Single(), Is.EqualTo(updating));
    Assert.That(queried1.Single(), Is.EqualTo(updating.CoreEntity));
    Assert.That(queried2.Single(), Is.EqualTo(updating));
    Assert.That(queried3.Single(), Is.EqualTo(updating));
    Assert.That(queried4, Is.Empty);
    Assert.That(tracker.Added.Single(), Is.EqualTo(ceam));
    Assert.That(tracker.Updated.Single(), Is.EqualTo(updating));
  }

  private CoreEntityAndMeta CreateMemTypeCEAM() {
    var core = new CoreMembershipType(new(Guid.NewGuid().ToString()), new(Guid.NewGuid().ToString()), nameof(CoreMembershipType)) {
      System = SYS,
      LastUpdateSystem = SYS,
      DateCreated = UtcDate.UtcNow,
      DateUpdated = UtcDate.UtcNow
    };
    return new CoreEntityAndMeta(core, new CoreStorageMeta(SYS, new(Guid.NewGuid().ToString()), CoreEntityTypeName.From<CoreMembershipType>(), core.CoreId, UtcDate.UtcNow, UtcDate.UtcNow, SYS));
  }

}