using Centazio.Test.Lib.InMemRepos;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseSimulationCoreStorageRepositoryTests {

  private static readonly SystemName SYS = SC.CRM_SYSTEM;
  private static readonly CoreEntityTypeName CORETYPE = CoreEntityTypeName.From<CoreMembershipType>();
  
  private InMemoryEpochTracker tracker = null!;
  private ISimulationCoreStorageRepository repo = null!;
  
  protected abstract Task<ISimulationCoreStorageRepository> GetRepository(InMemoryEpochTracker tracker);
  
  [SetUp] public async Task SetUp() {
    tracker = new InMemoryEpochTracker();
    repo = await GetRepository(tracker);
  }

  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public async Task Test_adding_new_entity() {
    var adding = CreateMemTypeCEAM();
    var added = await repo.Upsert(CORETYPE, [adding]);
    var single = await repo.GetMembershipType(adding.CoreEntity.CoreId);
    var queried1 = await repo.GetMembershipTypes();
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
    var added = await repo.Upsert(CORETYPE, [ceam]);
    TestingUtcDate.DoTick();
    
    var coreupdate = (CoreMembershipType) ceam.CoreEntity with { Name = nameof(Test_updating_entity)};
    var updating = ceam.Update(SYS, coreupdate, Helpers.TestingCoreEntityChecksum(coreupdate)); 
    var updated = await repo.Upsert(CORETYPE, [updating]);
    var single = await repo.GetMembershipType(ceam.CoreEntity.CoreId);
    var queried1 = await repo.GetMembershipTypes();
    var queried2 = await repo.GetExistingEntities(CORETYPE, [updating.CoreEntity.CoreId]);
    var queried3 = await repo.GetEntitiesToWrite(new("ignore exclude"), CORETYPE, UtcDate.UtcNow.AddSeconds(-1));
    var queried4 = await repo.GetEntitiesToWrite(new("ignore exclude"), CORETYPE, UtcDate.UtcNow);
    
    Assert.That(updating.Meta.DateUpdated, Is.EqualTo(UtcDate.UtcNow));
    Assert.That(added.Single(), Is.EqualTo(ceam));
    Assert.That(ceam, Is.Not.EqualTo(updating));
    Assert.That(single, Is.EqualTo(updating.CoreEntity));
    Assert.That(updated.Single(), Is.EqualTo(updating));
    Assert.That(queried1.Single(), Is.EqualTo(updating.CoreEntity));
    Assert.That(queried2.Single(), Is.EqualTo(updating));
    Assert.That(queried3.Single(), Is.EqualTo(updating));
    Assert.That(queried4, Is.Empty);
    Assert.That(tracker.Added.Single(), Is.EqualTo(ceam.CoreEntity));
    Assert.That(tracker.Updated.Single(), Is.EqualTo(updating.CoreEntity));
  }

  private CoreEntityAndMeta CreateMemTypeCEAM() {
    var id = new CoreEntityId(Guid.NewGuid().ToString());
    var core = new CoreMembershipType(id, CorrelationId.Build(SYS, new (Guid.NewGuid().ToString())), nameof(CoreMembershipType));
    return CoreEntityAndMeta.Create(SYS, SystemEntityTypeName.From<CrmMembershipType>(), new(Guid.NewGuid().ToString()), core, Helpers.TestingCoreEntityChecksum(core));
  }

}