using Centazio.Core.Entities.Ctl;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace centazio.core.tests.Stage;

public abstract class StagedEntityStoreDefaultTests {

  private static readonly string NAME = nameof(StagedEntityStoreDefaultTests);
  
  protected abstract Task<IStagedEntityStore> GetStore();
  
  private IStagedEntityStore store;
  
  [SetUp] public async Task SetUp() => store = await GetStore();
  [TearDown] public async Task TearDown() => await store.DisposeAsync();

  [Test] public async Task Test_saving_single_entity() {
    var dt = new TestingUtcDate();
    
    await store.Save(dt.Now, NAME, NAME, NAME);
    var fromnow = await store.Get(dt.Now, NAME, NAME);
    var minus1 =  await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Single(), Is.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME)));
  }
  
  [Test] public async Task Test_updating_single_entity() {
    var dt = new TestingUtcDate();
    
    await store.Save(dt.Now, NAME, NAME, NAME);
    var created = (await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    var updated = created with { DatePromoted = dt.Now.AddYears(1) };
    await store.Update(updated);
    var updated2 = (await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    
    Assert.That(updated2, Is.EqualTo(updated));
  }

  [Test] public async Task Test_saving_multiple_entities() {
    var dt = new TestingUtcDate();
    var count = 100;
    
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, count).Select(_ => NAME));
    var fromnow = await store.Get(dt.Now, NAME, NAME);
    var minus1 =  await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(count));
    Assert.That(minus1, Is.All.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME)));
  }

  [Test] public async Task Test_updating_multiple_entities() {
    var dt = new TestingUtcDate();
    var count = 100;
    
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, count).Select(_ => NAME));
    var fromnow = await store.Get(dt.Now, NAME, NAME);
    var minus1 =  await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(count));
    Assert.That(minus1, Is.All.EqualTo(new StagedEntity(NAME, NAME, dt.Now, NAME)));
  }

  [Test] public async Task Test_saving_multiple_large_entities() {
    var dt = new TestingUtcDate();
    var count = 100;
    var sz = 10000;
    var str = new String('*', sz);
    
    await store.Save(dt.Now, NAME, NAME, Enumerable.Range(0, count).Select(_ => str));
    var fromnow = await store.Get(dt.Now, NAME, NAME);
    var minus1 =  await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1.Count, Is.EqualTo(count));
    Assert.That(minus1, Is.All.EqualTo(new StagedEntity(NAME, NAME, dt.Now, str)));
  }
  
  [Test] public async Task Test_get_returns_expected() {
    var dt = new TestingUtcDate();
    var (start, staged1, staged2) = (dt.Now, dt.Tick(), dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    await store.Save(staged1, name1, name1, name1);
    await store.Save(staged2, name1, name1, name1);
    await store.Save(staged2, name2, name2, name2);
    await store.Save(staged2, name3, name3, name3);
    
    Assert.That(await store.Get(staged2, name1, name1), Is.Empty);
    Assert.That((await store.Get(start, name2, name1)), Is.Empty);
    Assert.That((await store.Get(start, name1, name2)), Is.Empty);
    Assert.That((await store.Get(start, name2, name3)), Is.Empty);
    Assert.That((await store.Get(start, name3, name2)), Is.Empty);

    Assert.That((await store.Get(staged1, name1, name1)).Single(), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1)));
    Assert.That((await store.Get(start, name1, name1)).Count, Is.EqualTo(2));
    Assert.That((await store.Get(staged1, name2, name2)).Single(), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2)));
    Assert.That((await store.Get(staged1, name3, name3)).Single(), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3)));
  }
  
  [Test] public async Task Test_delete_staged_before() {
    var dt = new TestingUtcDate();
    var (get_all, delete_all, staged1, staged2) = (dt.Now.AddHours(-1), dt.Now.AddHours(1), dt.Tick(), dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    await store.Save(staged1, name1, name1, name1);
    await store.Save(staged2, name1, name1, name1);
    await store.Save(staged2, name2, name2, name2);
    await store.Save(staged2, name3, name3, name3);
    
    await store.DeleteStagedBefore(staged2, name1, name1); // will delete name1@staged1
    Assert.That((await store.Get(get_all, name1, name1)).Single(), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1)));
    Assert.That((await store.Get(get_all, name2, name2)).Single(), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2)));
    Assert.That((await store.Get(get_all, name3, name3)).Single(), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1); // will delete remaining name1
    Assert.That(await store.Get(get_all, name1, name1), Is.Empty);
    Assert.That((await store.Get(get_all, name2, name2)).Single(), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2)));
    Assert.That((await store.Get(get_all, name3, name3)).Single(), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3)));
  }
    
  [Test] public async Task Test_delete_promoted_before() {
    var dt = new TestingUtcDate();
    var (get_all, delete_all, staged1, staged2, promoted2) = (dt.Now.AddHours(-1), dt.Now.AddHours(1), dt.Tick(), dt.Tick(), dt.Now.AddDays(1));
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    await store.Save(staged1, name1, name1, name1);
    await store.Save(staged2, name1, name1, name1);
    await store.Save(staged2, name2, name2, name2);
    await store.Save(staged2, name3, name3, name3);
    
    var all = (await store.Get(get_all, name1, name1))
        .Concat(await store.Get(get_all, name2, name2))
        .Concat(await store.Get(get_all, name3, name3)).Select(se => se with { DatePromoted = se.DateStaged.AddDays(1) });
    await store.Update(all);
    
    await store.DeletePromotedBefore(promoted2, name1, name1);
    Assert.That((await store.Get(get_all, name1, name1)).Single(), Is.EqualTo(new StagedEntity(name1, name1, staged2, name1, promoted2)));
    Assert.That((await store.Get(get_all, name2, name2)).Single(), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, promoted2)));
    Assert.That((await store.Get(get_all, name3, name3)).Single(), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, promoted2)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1); 
    Assert.That(await store.Get(get_all, name1, name1), Is.Empty);
    Assert.That((await store.Get(get_all, name2, name2)).Single(), Is.EqualTo(new StagedEntity(name2, name2, staged2, name2, promoted2)));
    Assert.That((await store.Get(get_all, name3, name3)).Single(), Is.EqualTo(new StagedEntity(name3, name3, staged2, name3, promoted2)));
  }
}