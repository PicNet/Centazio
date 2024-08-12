using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace centazio.core.tests.Stage;

public abstract class StagedEntityStoreDefaultTests {

  private const string NAME = nameof(StagedEntityStoreDefaultTests);
  private const int LARGE_BATCH_SIZE = 100;
  
  protected abstract Task<IStagedEntityStore> GetStore();
  
  private IStagedEntityStore store;
  private TestingUtcDate dt;
  
  [SetUp] public async Task SetUp() {
    store = await GetStore();
    dt = new TestingUtcDate();
  }

  [TearDown] public async Task TearDown() => await store.DisposeAsync();

  [Test] public async Task Test_saving_single_entity() {
    await store.Save(dt.Now, NAME, NAME, NAME);
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetSingleAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1, Is.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME)));
  }
  
  [Test] public async Task Test_updating_single_entity() {
    await store.Save(dt.Now, NAME, NAME, NAME);
    var created = (await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    var updated = created with { DatePromoted = dt.Now.AddYears(1) };
    await store.Update(updated);
    var updated2 = (await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    
    Assert.That(updated2, Is.EqualTo(updated));
  }

  [Test] public async Task Test_saving_multiple_entities() {
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => NAME));
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.All.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME)));
  }

  [Test] public async Task Test_updating_multiple_entities() {
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => NAME));
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.All.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME)));
  }

  [Test] public async Task Test_saving_multiple_large_entities() {
    var sz = 10000;
    var str = new String('*', sz);
    
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => str));
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.All.EqualTo(new StagedEntity(NAME, NAME, dt.Now, str)));
  }
  
  [Test] public async Task Test_get_returns_expected() {
    var (start, staged1, staged2) = (dt.Now, dt.Tick(), dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    
    await store.Save(staged1, name1, name1, name1);
    await store.Save(staged2, name1, name1, name1);
    await store.Save(staged2, name2, name2, name2);
    await store.Save(staged2, name3, name3, name3);
    await Assert.ThatAsync(() => GetAsSes(staged2, name1, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name1, name2), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name3), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name3, name2), Is.Empty);

    Assert.That(await GetSingleAsSes(staged1, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1)));
    await Assert.ThatAsync(async () => (await GetAsSes(start, name1, name1)).Count, Is.EqualTo(2));
    await Assert.ThatAsync(() => GetAsSes(staged1, name2, name2), Is.EqualTo(new List<StagedEntity> { new(name2, name2, staged2, name2) }));
    await Assert.ThatAsync(() => GetAsSes(staged1, name3, name3), Is.EqualTo(new List<StagedEntity> { new(name3, name3, staged2, name3) }));
  }
  
  [Test] public async Task Test_get_returns_expected_with_ignores() {
    var (start, staged1, staged2) = (dt.Now, dt.Tick(), dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    
    var notignore = new List<StagedEntity> {
      await store.Save(staged1, name1, name1, name1) with { Ignore = "" },
      await store.Save(staged2, name1, name1, name1) with { Ignore = " " },
      await store.Save(staged2, name2, name2, name2) with { Ignore = "\r" },
      await store.Save(staged2, name3, name3, name3) with { Ignore = null },
    };
    await store.Update(notignore);
    
    var toignore = new List<StagedEntity> {
      await store.Save(staged1.AddMinutes(1), name1, name1, name1) with { Ignore = nameof(StagedEntity.Ignore) },
      await store.Save(staged2.AddMinutes(1), name1, name1, name1) with { Ignore = nameof(StagedEntity.Ignore) },
      await store.Save(staged2.AddMinutes(1), name2, name2, name2) with { Ignore = nameof(StagedEntity.Ignore) },
      await store.Save(staged2.AddMinutes(1), name3, name3, name3) with { Ignore = nameof(StagedEntity.Ignore) }
    };
    await store.Update(toignore);
    
    await Assert.ThatAsync(() => GetAsSes(staged2, name1, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name1), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name1, name2), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name2, name3), Is.Empty);
    await Assert.ThatAsync(() => GetAsSes(start, name3, name2), Is.Empty);

    Assert.That(await GetSingleAsSes(staged1, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1)));
    await Assert.ThatAsync(async () => (await GetAsSes(start, name1, name1)).Count, Is.EqualTo(2));
    await Assert.ThatAsync(() => GetAsSes(staged1, name2, name2), Is.EqualTo(new List<StagedEntity> { new(name2, name2, staged2, name2) }));
    await Assert.ThatAsync(() => GetAsSes(staged1, name3, name3), Is.EqualTo(new List<StagedEntity> { new(name3, name3, staged2, name3) }));
  }
  
  [Test] public async Task Test_delete_staged_before() {
    var (get_all, delete_all, staged1, staged2) = (dt.Now.AddHours(-1), dt.Now.AddHours(1), dt.Tick(), dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    await store.Save(staged1, name1, name1, name1);
    await store.Save(staged2, name1, name1, name1);
    await store.Save(staged2, name2, name2, name2);
    await store.Save(staged2, name3, name3, name3);

    await store.DeleteStagedBefore(staged2, name1, name1); // will delete name1@staged1
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1); // will delete remaining name1
    await Assert.ThatAsync(() => store.Get(get_all, name1, name1), Is.Empty);
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3)));
  }
  
  [Test] public async Task Test_delete_large_batch() {
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => NAME));
    await store.DeleteStagedBefore(dt.Tick(), NAME, NAME); 
    await Assert.ThatAsync(async () => await GetAsSes(dt.Now.AddHours(-1), NAME, NAME), Is.Empty);
  }
    
  [Test] public async Task Test_delete_promoted_before() {
    var (get_all, delete_all, staged1, staged2, promoted2) = (dt.Now.AddHours(-1), dt.Now.AddHours(1), dt.Tick(), dt.Tick(), dt.Now.AddDays(1));
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    await store.Save(staged1, name1, name1, name1);
    await store.Save(staged2, name1, name1, name1);
    await store.Save(staged2, name2, name2, name2);
    await store.Save(staged2, name3, name3, name3);
    
    var all = (await store.Get(get_all, name1, name1))
        .Concat(await store.Get(get_all, name2, name2))
        .Concat(await store.Get(get_all, name3, name3))
        .Select(se => se with { DatePromoted = se.DateStaged.AddDays(1) });
    await store.Update(all);
    
    await store.DeletePromotedBefore(promoted2, name1, name1);
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name1, name1), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1, promoted2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, promoted2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, promoted2)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1); 
    await Assert.ThatAsync(() => GetAsSes(get_all, name1, name1), Is.Empty);
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name2, name2), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, promoted2)));
    await Assert.ThatAsync(async () => await GetSingleAsSes(get_all, name3, name3), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, promoted2)));
  }
  
  private async Task<StagedEntity> GetSingleAsSes(DateTime staged, SystemName source, ObjectName obj) =>
    (await GetAsSes(staged, source, obj)).Select(se => se.CloneNew()).Single();
  
  private async Task<List<StagedEntity>> GetAsSes(DateTime staged, SystemName source, ObjectName obj) =>
    (await store.Get(staged, source, obj)).Select(se => se.CloneNew()).ToList();
}