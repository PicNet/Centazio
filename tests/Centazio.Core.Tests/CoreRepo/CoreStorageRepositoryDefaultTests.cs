using Centazio.Core.Tests.IntegrationTests;

namespace Centazio.Core.Tests.CoreRepo;

public abstract class CoreStorageRepositoryDefaultTests(bool supportExpressions) {

  protected bool SupportsExpressionBasedQuery { get; } = supportExpressions;
  
  private ICoreStorageRepository repo = null!;
  
  [SetUp] public async Task SetUp() => repo = await GetRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();
  
  protected abstract Task<ICoreStorageRepository> GetRepository();
  
  [Test] public async Task Test_get_missing_entity_throws_exception() {
    Assert.ThrowsAsync<Exception>(() => repo.Get<CoreEntity>("invalid"));
    await repo.Upsert([ TestingFactories.NewCoreCust("", "") ]);
    Assert.ThrowsAsync<Exception>(() => repo.Get<CoreEntity>("invalid"));
  }

  [Test] public async Task Test_insert_get_update_get() {
    var created = TestingFactories.NewCoreCust("N1", "N1");
    await repo.Upsert([ created ]);
    var retreived1 = await repo.Get<CoreEntity>(created.Id);
    var list1 = await QueryAll();
    var updated = retreived1 with { FirstName = "N2" };
    await repo.Upsert([ updated ]);
    var retreived2 = await repo.Get<CoreEntity>(created.Id);
    var list2 = await QueryAll();
    
    Assert.That(retreived1, Is.EqualTo(created));
    Assert.That(list1, Is.EquivalentTo(new [] { created }));
    Assert.That(retreived1, Is.Not.EqualTo(updated));
    Assert.That(retreived2, Is.EqualTo(updated));
    Assert.That(list2, Is.EquivalentTo(new [] { updated }));
  }
  
  [Test] public async Task Test_batch_upsert() {
    var batch1 = new [] { TestingFactories.NewCoreCust("N1", "N1"), TestingFactories.NewCoreCust("N2", "N2") };
    await repo.Upsert(batch1);
    var list1 = await QueryAll();
    
    var batch2 = new [] { 
      batch1[0] with { FirstName = "Updated entity" }, 
      TestingFactories.NewCoreCust("N3", "N3") };
    await repo.Upsert(batch2);
    var list2 = await QueryAll();
    
    Assert.That(list1, Is.EquivalentTo(batch1));
    Assert.That(list2, Is.EquivalentTo(new [] { batch2[0], batch1[1], batch2[1] }));
  }
  
  [Test] public async Task Test_query() {
    var data = Enumerable.Range(0, 100).Select(idx => TestingFactories.NewCoreCust($"{idx}", $"{idx}")).ToList();
    await repo.Upsert(data);
    
    var (all, even, odd) = (await QueryAll(), await QueryEvenOdd(true), await QueryEvenOdd(false));

    Assert.That(all, Is.EquivalentTo(data));
    Assert.That(even, Has.Count.EqualTo(50));
    Assert.That(odd, Has.Count.EqualTo(50));
    Assert.That(all, Is.EquivalentTo(even.Concat(odd)));
  }
  
  
  private async Task<List<CoreEntity>> QueryAll() {
    return (SupportsExpressionBasedQuery 
        ? await repo.Query<CoreEntity>(e => true)
        : await repo.Query<CoreEntity>($"SELECT * FROM {nameof(CoreEntity)}")).ToList();
  }

  private async Task<List<CoreEntity>> QueryEvenOdd(bool even) {
    return (SupportsExpressionBasedQuery 
        ? await repo.Query<CoreEntity>(e => Int32.Parse(e.FirstName) % 2 == (even ? 0 : 1))
        : await repo.Query<CoreEntity>($"SELECT * FROM {nameof(CoreEntity)} WHERE FirstName % 2 = " + (even ? 0 : 1))).ToList();
  }
}