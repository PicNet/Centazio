using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public abstract class StagedEntityStoreDefaultTests {

  protected const string NAME = nameof(StagedEntityStoreDefaultTests);
  protected const int LARGE_BATCH_SIZE = 100;
  
  protected abstract Task<IStagedEntityStore> GetStore(int limit=0, Func<string, string>? checksum = null);
  protected IStagedEntityStore store;
  protected TestingUtcDate dt;
  
  [SetUp] public async Task SetUp() {
    store = await GetStore(0, Hash);
    dt = (TestingUtcDate) UtcDate.Utc;
  }

  [TearDown] public async Task TearDown() => await store.DisposeAsync();

  [Test] public async Task Test_saving_single_entity() {
    await store.Stage(dt.Now, NAME, NAME, NAME);
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetSingleAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1, Is.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME, Hash(NAME))));
  }
  
  [Test] public async Task Test_updating_single_entity() {
    await store.Stage(dt.Now, NAME, NAME, NAME);
    var created = (await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    var updated = created with { DatePromoted = dt.Now.AddYears(1) };
    await store.Update(updated);
    var updated2 = (await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    
    Assert.That(updated2, Is.EqualTo(updated));
  }

  [Test] public async Task Test_saving_multiple_entities() {
    await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => NAME + idx));
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(NAME, NAME, dt.Now, NAME + idx, Hash(NAME + idx)))));
  }

  [Test] public async Task Test_updating_multiple_entities() {
    await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => NAME + idx));
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(NAME, NAME, dt.Now, NAME + idx, Hash(NAME + idx)))));
  }

  [Test] public async Task Test_saving_multiple_large_entities() {
    var sz = 10000;
    var str = new String('*', sz);
    
    await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => str + idx));
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(NAME, NAME, dt.Now, str + idx, Hash(str + idx)))));
  }
  
  [Test] public async Task Test_get_returns_expected() {
    var (start, staged1, staged2) = (dt.Now, dt.Tick(), dt.Tick());
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    
    await store.Stage(staged1, name1, name1, name1);
    await store.Stage(staged2, name1, name1, data2);
    await store.Stage(staged2, name2, name2, name2);
    await store.Stage(staged2, name3, name3, name3);
    
    await Assert.ThatAsync(() => GetAsSes(staged2, name1, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name1, name2), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name3), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name3, name2), Is.Empty);

    Assert.That(await GetSingleAsSes(staged1, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, data2, Hash(data2))));
    await Assert.ThatAsync(async () => (await GetAsSes(start, name1, name1)).Count, Is.EqualTo(2));
    await Assert.ThatAsync(() => GetAsSes(staged1, name2, name2), Is.EqualTo(new List<StagedEntity> { new(name2, name2, staged2, name2, Hash(name2)) }));
    await Assert.ThatAsync(() => GetAsSes(staged1, name3, name3), Is.EqualTo(new List<StagedEntity> { new(name3, name3, staged2, name3, Hash(name3)) }));
  }
  
  [Test] public async Task Test_get_returns_expected_with_ignores() {
    var (start, staged1, staged2) = (dt.Now, dt.Tick(), dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    
    var notignore = new List<StagedEntity> {
      await Create(staged1, name1, "not ignore: 1.1", ""),
      await Create(staged2, name1, "not ignore: 1.2", " "),
      await Create(staged2, name2, "not ignore: 2", "\r"),
      await Create(staged2, name3, "not ignore: 3", null)
    };
    await store.Update(notignore);
    
    var toignore = new List<StagedEntity> {
      await Create(staged1.AddMinutes(1), name1, "ignore: 1.1", "ignore: 1.1"),
      await Create(staged2.AddMinutes(1), name1, "ignore: 1.2", "ignore: 1.2"),
      await Create(staged2.AddMinutes(1), name2, "ignore: 2", "ignore: 2"),
      await Create(staged2.AddMinutes(1), name3, "ignore: 3", "ignore: 3")
    };
    await store.Update(toignore);
    
    await Assert.ThatAsync(() => GetAsSes(staged2, name1, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name1, name2), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name3), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name3, name2), Is.Empty);

    await Assert.ThatAsync(() => GetSingleAsSes(staged1, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, "not ignore: 1.2", Hash("not ignore: 1.2"))));
    await Assert.ThatAsync(async () => (await GetAsSes(start, name1, name1)).Count, Is.EqualTo(2));
    await Assert.ThatAsync(() => GetAsSes(staged1, name2, name2), Is.EqualTo(new List<StagedEntity> { new(name2, name2, staged2, "not ignore: 2", Hash("not ignore: 2")) }));
    await Assert.ThatAsync(() => GetAsSes(staged1, name3, name3), Is.EqualTo(new List<StagedEntity> { new(name3, name3, staged2, "not ignore: 3", Hash("not ignore: 3")) }));
    
    async Task<StagedEntity> Create(DateTime dtstaged, string name, string data, string? ignore) {
      var staged = await store.Stage(dtstaged, name, name, data) ?? throw new Exception();
      return staged with { Ignore = ignore};
    }
  }
  
  [Test] public async Task Test_get_returns_oldest_first_page_as_expected() {
    var limit = 10;
    var limstore = await GetStore(limit, Hash);
    var start = dt.Now;
    var created = new List<StagedEntity>();
    foreach (var idx in Enumerable.Range(0, 25)) created.Add(await limstore.Stage(dt.Tick(), NAME, NAME, NAME + idx) ?? throw new Exception());
    
    var exppage1 = created.Take(limit).ToList();
    var page1 = await limstore.Get(start, NAME, NAME);
    
    var exppage2 = created.Skip(limit).Take(limit).ToList();
    var page2 = await limstore.Get(exppage1.Last().DateStaged, NAME, NAME);
    
    var exppage3 = created.Skip(limit * 2).Take(limit).ToList();
    var page3 = await limstore.Get(exppage2.Last().DateStaged, NAME, NAME);
    
    var page4 = await limstore.Get(exppage3.Last().DateStaged, NAME, NAME);
    
    Assert.That(page1, Is.EquivalentTo(exppage1));
    Assert.That(page2, Is.EquivalentTo(exppage2));
    Assert.That(page3, Is.EquivalentTo(exppage3));
    Assert.That(page4, Is.Empty);
  }
  
  [Test] public async Task Test_delete_staged_before() {
    var (get_all, delete_all, staged1, staged2) = (dt.Now.AddHours(-1), dt.Now.AddHours(1), dt.Tick(), dt.Tick());
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    await store.Stage(staged1, name1, name1, name1);
    await store.Stage(staged2, name1, name1, data2);
    await store.Stage(staged2, name2, name2, name2);
    await store.Stage(staged2, name3, name3, name3);

    await store.DeleteStagedBefore(staged2, name1, name1); // will delete name1@staged1
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, data2, Hash(data2))));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, Hash(name2))));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, Hash(name3))));
    
    await store.DeleteStagedBefore(delete_all, name1, name1); // will delete remaining name1
    await Assert.ThatAsync(() => store.Get(get_all, name1, name1), Is.Empty);
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, Hash(name2))));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, Hash(name3))));
  }
  
  [Test] public async Task Test_delete_large_batch() {
    await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => NAME));
    await store.DeleteStagedBefore(dt.Tick(), NAME, NAME); 
    await Assert.ThatAsync(async () => await GetAsSes(dt.Now.AddHours(-1), NAME, NAME), Is.Empty);
  }
    
  [Test] public async Task Test_delete_promoted_before() {
    var (get_all, delete_all, staged1, staged2, promoted2) = (dt.Now.AddHours(-1), dt.Now.AddHours(1), dt.Tick(), dt.Tick(), dt.Now.AddDays(1));
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    await store.Stage(staged1, name1, name1, name1);
    await store.Stage(staged2, name1, name1, data2);
    await store.Stage(staged2, name2, name2, name2);
    await store.Stage(staged2, name3, name3, name3);
    
    var all = (await store.Get(get_all, name1, name1))
        .Concat(await store.Get(get_all, name2, name2))
        .Concat(await store.Get(get_all, name3, name3))
        .Select(se => se with { DatePromoted = se.DateStaged.AddDays(1) });
    await store.Update(all);
    
    await store.DeletePromotedBefore(promoted2, name1, name1);
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, data2, Hash(data2), promoted2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, Hash(name2), promoted2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, Hash(name3), promoted2)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1); 
    await Assert.ThatAsync(() => GetAsSes(get_all, name1, name1), Is.Empty);
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, Hash(name2), promoted2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, Hash(name3), promoted2)));
  }
  
  [Test] public async Task Test_stage_single_ignores_duplicates() {
    var (data, stageddt) = (Guid.NewGuid().ToString(), dt.Tick());
    var staged = await store.Stage(stageddt, NAME, NAME, data);
    var duplicate = await store.Stage(dt.Tick(), NAME, NAME, data);
    
    var expected = new StagedEntity(NAME, NAME, stageddt, data, Hash(data));
    Assert.That(staged?.CloneNew(), Is.EqualTo(expected));
    await Assert.ThatAsync(() => GetAsSes(dt.Now.AddYears(-1), NAME, NAME), Is.EquivalentTo(new [] {expected}));
    Assert.That(duplicate, Is.Null);
  }
  
  [Test] public async Task Test_staging_multiple_entities_ignores_duplicates() {
    var (start, half) = (dt.Now.AddSeconds(1), LARGE_BATCH_SIZE /  2);
    var staged = (await store.Stage(dt.Tick(), NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => NAME + idx % half)))
        .Select(e => e.CloneNew()).ToList();
    var staged2 = await GetAsSes(dt.Now.AddYears(-1), NAME, NAME);
    
    Assert.That(staged, Has.Count.EqualTo(half));
    Assert.That(staged, Is.EquivalentTo(staged2));
    Assert.That(staged, Is.EquivalentTo(Enumerable.Range(0, half).Select(idx => new StagedEntity(NAME, NAME, start, NAME + idx, Hash(NAME + idx)))));
  }
  
  public string Hash(string str) => TestingFactories.TestingChecksum(str);
  
  
  private async Task<StagedEntity> GetSingleAsSes(DateTime after, SystemName source, ObjectName obj) =>
    (await GetAsSes(after, source, obj)).Select(se => se.CloneNew()).Single();
  
  private async Task<List<StagedEntity>> GetAsSes(DateTime after, SystemName source, ObjectName obj) =>
    (await store.Get(after, source, obj)).Select(se => se.CloneNew()).ToList();
}