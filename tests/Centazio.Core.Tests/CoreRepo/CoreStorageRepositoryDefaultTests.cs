using Centazio.Core.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.CoreRepo;

public abstract class CoreStorageRepositoryDefaultTests(bool supportExpressions) {

  protected bool SupportsExpressionBasedQuery { get; } = supportExpressions;
  
  private ICoreStorageRepository repo = null!;
  
  [SetUp] public async Task SetUp() {
    repo = await GetRepository();
  }
  
  [TearDown] public async Task TearDown() {
    await repo.DisposeAsync();
  }
  
  protected abstract Task<ICoreStorageRepository> GetRepository();
  
  [Test] public async Task Test_get_missing_entity_throws_exception() {
    Assert.ThrowsAsync<Exception>(() => repo.Get<CoreCustomer>("invalid"));
    await repo.Upsert(new CoreCustomer(Guid.NewGuid().ToString(), "", "", DateOnly.MinValue, UtcDate.UtcNow));
    Assert.ThrowsAsync<Exception>(() => repo.Get<CoreCustomer>("invalid"));
  }
  
  [Test] public async Task Test_insert_get_update_get() {
    var id = Guid.NewGuid().ToString();
    var created = new CoreCustomer(id, "N1", "N1", DateOnly.MinValue, UtcDate.UtcNow);
    await repo.Upsert(created);
    var retreived1 = await repo.Get<CoreCustomer>(id);
    var list1 = await QueryAll();
    var updated = retreived1 with { FirstName = "N2" };
    await repo.Upsert(updated);
    var retreived2 = await repo.Get<CoreCustomer>(id);
    var list2 = await QueryAll();
    
    Assert.That(retreived1, Is.EqualTo(created));
    Assert.That(list1, Is.EquivalentTo(new [] { created }));
    Assert.That(retreived1, Is.Not.EqualTo(updated));
    Assert.That(retreived2, Is.EqualTo(updated));
    Assert.That(list2, Is.EquivalentTo(new [] { updated }));
  }
  
  [Test] public async Task Test_batch_upsert() {
    var batch1 = new [] { 
      new CoreCustomer(Guid.NewGuid().ToString(), "N1", "N1", DateOnly.MinValue, UtcDate.UtcNow),
      new CoreCustomer(Guid.NewGuid().ToString(), "N2", "N2", DateOnly.MinValue, UtcDate.UtcNow) };
    
    await repo.Upsert(batch1);
    var list1 = await QueryAll();
    
    var batch2 = new [] { 
      new CoreCustomer(batch1[0].Id, "N1.1", "N1.1", DateOnly.MinValue, UtcDate.UtcNow),
      new CoreCustomer(Guid.NewGuid().ToString(), "N3", "N3", DateOnly.MinValue, UtcDate.UtcNow) };
    
    await repo.Upsert(batch2);
    var list2 = await QueryAll();
    
    Assert.That(list1, Is.EquivalentTo(batch1));
    Assert.That(list2, Is.EquivalentTo(new [] { batch2[0], batch1[1], batch2[1] }));
  }
  
  [Test] public async Task Test_query() {
    var data = Enumerable.Range(0, 100).Select(idx => new CoreCustomer(idx.ToString(), $"N{idx}", $"N{idx}", DateOnly.FromDateTime(TestingDefaults.DefaultStartDt.AddDays(idx)), UtcDate.UtcNow)).ToList();
    await repo.Upsert(data);
    
    var (all, even, odd) = (await QueryAll(), await QueryEvenOdd(true), await QueryEvenOdd(false));
    
    Assert.That(all, Is.EquivalentTo(data));
    Assert.That(even, Has.Count.EqualTo(50));
    Assert.That(odd, Has.Count.EqualTo(50));
    Assert.That(all, Is.EquivalentTo(even.Concat(odd)));
  }
  
  private async Task<List<CoreCustomer>> QueryAll() {
    return (SupportsExpressionBasedQuery 
        ? await repo.Query<CoreCustomer>(e => true)
        : await repo.Query<CoreCustomer>($"SELECT * FROM {nameof(CoreCustomer)}")).ToList();
  }

  private async Task<List<CoreCustomer>> QueryEvenOdd(bool even) {
    return (SupportsExpressionBasedQuery 
        ? await repo.Query<CoreCustomer>(e => Int32.Parse(e.Id) % 2 == (even ? 0 : 1))
        : await repo.Query<CoreCustomer>($"SELECT * FROM {nameof(CoreCustomer)} WHERE Id % 2 = " + (even ? 0 : 1))).ToList();
  }
}
