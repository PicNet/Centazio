using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseCoreStorageRepositoryTests {
  
  private ITestingCoreStorage repo = null!;
  
  [SetUp] public async Task SetUp() => repo = await GetRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();
  
  protected abstract Task<ITestingCoreStorage> GetRepository();
  
  [Test] public async Task Test_get_missing_entity_throws_exception() {
    Assert.ThrowsAsync<Exception>(() => repo.GetExistingEntities(C.CoreEntityName, [new("invalid")]));
    await DoUpsert(TestingFactories.NewCoreEntity(String.Empty, String.Empty));
    Assert.ThrowsAsync<Exception>(() => repo.GetExistingEntities(C.CoreEntityName, [new("invalid")]));
  }

  [Test] public async Task Test_insert_get_update_get() {
    var created = TestingFactories.NewCoreEntity("N1", "N1");
    await DoUpsert(created);
    var retreived1 = await GetSingle(created.CoreEntity.CoreId);
    var list1 = await repo.GetAllCoreEntities();
    var updated = created with {
      CoreEntity = created.As<CoreEntity>() with { FirstName = "N2" } 
    };
    await DoUpsert(updated);
    var retreived2 = await GetSingle(created.CoreEntity.CoreId);
    var list2 = await repo.GetAllCoreEntities();
    
    Assert.That(retreived1, Is.EqualTo(created.CoreEntity));
    Assert.That(list1, Is.EquivalentTo([created.CoreEntity]));
    Assert.That(retreived1, Is.Not.EqualTo(updated.CoreEntity));
    Assert.That(retreived2, Is.EqualTo(updated.CoreEntity));
    Assert.That(list2, Is.EquivalentTo([updated.CoreEntity]));
  }
  
  [Test] public async Task Test_batch_upsert() {
    var batch1 = new List<CoreEntityAndMeta> { TestingFactories.NewCoreEntity("N1", "N1"), TestingFactories.NewCoreEntity("N2", "N2") };
    await DoUpsert(batch1);
    var list1 = await repo.GetAllCoreEntities();
    
    var batch2 = new List<CoreEntityAndMeta> { 
      batch1[0] with {
        CoreEntity = batch1[0].As<CoreEntity>() with { 
          FirstName = "Updated entity" 
        }
      }, 
      TestingFactories.NewCoreEntity("N3", "N3") };
    await DoUpsert(batch2);
    var list2 = await repo.GetAllCoreEntities();
    
    Assert.That(list1, Is.EquivalentTo(batch1.Select(e => e.CoreEntity)));
    Assert.That(list2, Is.EquivalentTo([batch2[0].CoreEntity, batch1[1].CoreEntity, batch2[1].CoreEntity]));
  }
  
  [Test] public async Task Test_query() {
    var data = Enumerable.Range(0, 100)
        .Select(idx => TestingFactories.NewCoreEntity($"{idx}", $"{idx}"))
        .ToList();
    await DoUpsert(data);
    
    var all = await repo.GetAllCoreEntities();
    Assert.That(all, Is.EquivalentTo(data.Select(e => e.CoreEntity)));
  }
  
  private async Task<CoreEntity> GetSingle(CoreEntityId coreid) => (CoreEntity) (await repo.GetExistingEntities(C.CoreEntityName, [coreid])).Single().CoreEntity;
  
  private Task DoUpsert(CoreEntityAndMeta coreent) => DoUpsert([coreent]);
  
  private Task DoUpsert(List<CoreEntityAndMeta> coreents) {
    coreents.ForEach(ValidateEntityPreUpsert);
    return repo.Upsert(C.CoreEntityName, coreents);
  }

  private void ValidateEntityPreUpsert(CoreEntityAndMeta coreent) {
    ArgumentNullException.ThrowIfNull(coreent.Meta.CoreId);
    ArgumentNullException.ThrowIfNull(coreent.Meta.OriginalSystem);
    ArgumentNullException.ThrowIfNull(coreent.Meta.OriginalSystemId);
    ArgumentNullException.ThrowIfNull(coreent.Meta.LastUpdateSystem);
    ArgumentOutOfRangeException.ThrowIfEqual(coreent.Meta.DateCreated, DateTime.MinValue);
    ArgumentOutOfRangeException.ThrowIfEqual(coreent.Meta.DateUpdated, DateTime.MinValue);
  }
}