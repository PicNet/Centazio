using Centazio.Core;
using Centazio.Core.CoreRepo;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseCoreStorageRepositoryTests(bool supportExpressions) {

  protected bool SupportsExpressionBasedQuery { get; } = supportExpressions;
  
  private ICoreStorageWithQuery repo = null!;
  
  [SetUp] public async Task SetUp() => repo = await GetRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();
  
  protected abstract Task<ICoreStorageWithQuery> GetRepository();
  
  [Test] public async Task Test_get_missing_entity_throws_exception() {
    Assert.ThrowsAsync<Exception>(() => repo.GetExistingEntities(Constants.CoreEntityName, [new("invalid")]));
    await DoUpsert(TestingFactories.NewCoreCust(String.Empty, String.Empty));
    Assert.ThrowsAsync<Exception>(() => repo.GetExistingEntities(Constants.CoreEntityName, [new("invalid")]));
  }

  [Test] public async Task Test_insert_get_update_get() {
    var created = TestingFactories.NewCoreCust("N1", "N1");
    await DoUpsert(created);
    var retreived1 = await GetSingle(created.CoreId);
    var list1 = await QueryAll();
    var updated = retreived1 with { FirstName = "N2" };
    await DoUpsert(updated);
    var retreived2 = await GetSingle(created.CoreId);
    var list2 = await QueryAll();
    
    Assert.That(retreived1, Is.EqualTo(created));
    Assert.That(list1, Is.EquivalentTo(new [] { created }));
    Assert.That(retreived1, Is.Not.EqualTo(updated));
    Assert.That(retreived2, Is.EqualTo(updated));
    Assert.That(list2, Is.EquivalentTo(new [] { updated }));
  }
  
  [Test] public async Task Test_batch_upsert() {
    var batch1 = new List<ICoreEntity> { TestingFactories.NewCoreCust("N1", "N1"), TestingFactories.NewCoreCust("N2", "N2") };
    await DoUpsert(batch1);
    var list1 = await QueryAll();
    
    var batch2 = new List<ICoreEntity> { 
      (CoreEntity) batch1[0] with { FirstName = "Updated entity" }, 
      TestingFactories.NewCoreCust("N3", "N3") };
    await DoUpsert(batch2);
    var list2 = await QueryAll();
    
    Assert.That(list1, Is.EquivalentTo(batch1));
    Assert.That(list2, Is.EquivalentTo(new [] { batch2[0], batch1[1], batch2[1] }));
  }
  
  [Test] public async Task Test_query() {
    var data = Enumerable.Range(0, 100)
        .Select(idx => (ICoreEntity) TestingFactories.NewCoreCust($"{idx}", $"{idx}"))
        .ToList();
    await DoUpsert(data);
    
    var all = await QueryAll();
    Assert.That(all, Is.EquivalentTo(data));
  }
  
  private async Task<CoreEntity> GetSingle(CoreEntityId coreid) => (await repo.GetExistingEntities(Constants.CoreEntityName, [coreid])).Cast<CoreEntity>().Single();
  
  private Task DoUpsert(ICoreEntity coreent) => DoUpsert([coreent]);
  private Task DoUpsert(List<ICoreEntity> coreents) {
    coreents.ForEach(ValidateEntityPreUpsert);
    return repo.Upsert(Constants.CoreEntityName, coreents.Select(e => (e, Helpers.TestingCoreEntityChecksum(e))).ToList());
  }

  private void ValidateEntityPreUpsert(ICoreEntity coreent) {
    ArgumentNullException.ThrowIfNull(coreent.CoreId);
    ArgumentNullException.ThrowIfNull(coreent.System);
    ArgumentNullException.ThrowIfNull(coreent.SystemId);
    ArgumentNullException.ThrowIfNull(coreent.LastUpdateSystem);
    ArgumentOutOfRangeException.ThrowIfEqual(coreent.DateCreated, DateTime.MinValue);
    ArgumentOutOfRangeException.ThrowIfEqual(coreent.DateUpdated, DateTime.MinValue);
  }

  private async Task<List<CoreEntity>> QueryAll() {
    var results = (SupportsExpressionBasedQuery 
        ? await repo.Query<CoreEntity>(Constants.CoreEntityName, e => true)
        : await repo.Query<CoreEntity>(Constants.CoreEntityName, $"SELECT * FROM {nameof(CoreEntity)}")).ToList();
    return results.ToList();
  }
}