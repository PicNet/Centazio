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
    Assert.That(minus1, Is.EqualTo(new StagedEntity(minus1.Id, NAME, NAME, dt.Now, NAME, Hash(NAME))));
  }
  
  [Test] public async Task Test_updating_single_entity() {
    await store.Stage(dt.Now, NAME, NAME, NAME);
    var created = (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    var updated = created with { DatePromoted = dt.Now.AddYears(1) };
    await store.Update(updated);
    var updated2 = (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    
    Assert.That(updated2, Is.EqualTo(updated));
  }

  [Test] public async Task Test_saving_multiple_entities() {
    var staged = (await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => idx.ToString())) ?? throw new Exception()).
        OrderBy(e => Int32.Parse(e.Data)).
        ToList();
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 =  (await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(minus1[idx].Id, NAME, NAME, dt.Now, idx.ToString(), Hash(idx)))));
  }

  [Test] public async Task Test_updating_multiple_entities() {
    var staged = (await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => idx.ToString())) ?? throw new Exception())
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var fromnow = await GetAsSes(dt.Now, NAME, NAME);
    var minus1 = (await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.GroupBy(e => e.Checksum).Count(), Is.EqualTo(LARGE_BATCH_SIZE), "has duplicate checksums");
    Assert.That(staged, Has.Count.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Has.Count.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(minus1[idx].Id, NAME, NAME, dt.Now, idx.ToString(), Hash(idx)))));
  }

  [Test] public async Task Test_saving_multiple_large_entities() {
    var sz = 10000;
    var str = new String('*', sz) + "_";
    
    var staged = (await store.Stage(dt.Now, NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => str + idx)) ?? throw new Exception())
        .Select(e => e.CloneNew() with { Data = e.Data.Split('_')[1] }) // make it easier to debug without all the noise
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var fromnow = (await GetAsSes(dt.Now, NAME, NAME))
        .Select(e => e with { Data = e.Data.Split('_')[1] })
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var minus1 =  (await GetAsSes(dt.Now.AddMilliseconds(-1), NAME, NAME))
        .Select(e => e with { Data = e.Data.Split('_')[1] })
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.GroupBy(e => e.Checksum).Count(), Is.EqualTo(LARGE_BATCH_SIZE), "has duplicate checksums");
    Assert.That(staged.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(staged));
    var exp = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(minus1[idx].Id, NAME, NAME, dt.Now, str + idx, Hash(str + idx)))
        .Select(e => e with { Data = e.Data.Split('_')[1] })
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    Assert.That(minus1, Is.EquivalentTo(exp));
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
    
    var se1_2 = await GetSingleAsSes(staged1, name1, name1);
    var ses1 = await GetAsSes(start, name1, name1);
    var ses2 = await GetAsSes(staged1, name2, name2);
    var ses3 = await GetAsSes(staged1, name3, name3);
    Assert.That(ses1, Has.Count.EqualTo(2));
    Assert.That(se1_2, Is.EqualTo(new StagedEntity(se1_2.Id, name1, name1, staged2, data2, Hash(data2))));
    Assert.That(ses2, Is.EquivalentTo(new List<StagedEntity> { new(ses2.Single().Id, name2, name2, staged2, name2, Hash(name2)) }));
    Assert.That(ses3, Is.EquivalentTo(new List<StagedEntity> { new(ses3.Single().Id, name3, name3, staged2, name3, Hash(name3)) }));
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

    var ses1 = await GetAsSes(start, name1, name1);
    var se1_2 = await GetSingleAsSes(staged1, name1, name1);
    var ses2 = await GetAsSes(staged1, name2, name2);
    var ses3 = await GetAsSes(staged1, name3, name3);
    Assert.That(se1_2, Is.EqualTo(new StagedEntity(se1_2.Id, name1, name1, staged2, "not ignore: 1.2", Hash("not ignore: 1.2"))));
    Assert.That(ses1, Has.Count.EqualTo(2));
    Assert.That(ses2, Is.EquivalentTo(new List<StagedEntity> { new(ses2.Single().Id, name2, name2, staged2, "not ignore: 2", Hash("not ignore: 2")) }));
    Assert.That(ses3, Is.EquivalentTo(new List<StagedEntity> { new(ses3.Single().Id, name3, name3, staged2, "not ignore: 3", Hash("not ignore: 3")) }));
    
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
    foreach (var idx in Enumerable.Range(0, 25)) created.Add(await limstore.Stage(dt.Tick(), NAME, NAME, idx.ToString()) ?? throw new Exception());
    
    var exppage1 = created.Take(limit).ToList();
    var page1 = await limstore.GetAll(start, NAME, NAME);
    
    var exppage2 = created.Skip(limit).Take(limit).ToList();
    var page2 = await limstore.GetAll(exppage1.Last().DateStaged, NAME, NAME);
    
    var exppage3 = created.Skip(limit * 2).Take(limit).ToList();
    var page3 = await limstore.GetAll(exppage2.Last().DateStaged, NAME, NAME);
    
    var page4 = await limstore.GetAll(exppage3.Last().DateStaged, NAME, NAME);
    
    Assert.That(page1, Is.EquivalentTo(exppage1));
    Assert.That(page2, Is.EquivalentTo(exppage2));
    Assert.That(page3, Is.EquivalentTo(exppage3));
    Assert.That(page4, Is.Empty);
  }
  
  [Test] public async Task Test_delete_staged_before() {
    var (get_all, delete_all, staged1, staged2) = (dt.Today, dt.Now.AddHours(1), dt.Tick(), dt.Tick());
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    await store.Stage(staged1, name1, name1, name1);
    await store.Stage(staged2, name1, name1, data2);
    await store.Stage(staged2, name2, name2, name2);
    await store.Stage(staged2, name3, name3, name3);

    await store.DeleteStagedBefore(staged2, name1, name1); // will delete name1@staged1
    var se1 = await GetSingleAsSes(get_all, name1, name1);
    var se2 = await GetSingleAsSes(get_all, name2, name2);
    var se3 = await GetSingleAsSes(get_all, name3, name3);
    
    Assert.That(se1, Is.EqualTo(new StagedEntity(se1.Id, name1, name1, staged2, data2, Hash(data2))));
    Assert.That(se2, Is.EqualTo(new StagedEntity(se2.Id, name2, name2, staged2, name2, Hash(name2))));
    Assert.That(se3, Is.EqualTo(new StagedEntity(se3.Id, name3, name3, staged2, name3, Hash(name3))));
   
    await store.DeleteStagedBefore(delete_all, name1, name1); // will delete remaining name1
    await Assert.ThatAsync(() => store.GetAll(get_all, name1, name1), Is.Empty);
    var se22 = await GetSingleAsSes(get_all, name2, name2);
    var se23 = await GetSingleAsSes(get_all, name3, name3);
    Assert.That(se22, Is.EqualTo(new StagedEntity(se22.Id, name2, name2, staged2, name2, Hash(name2))));
    Assert.That(se23, Is.EqualTo(new StagedEntity(se23.Id, name3, name3, staged2, name3, Hash(name3))));
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
    
    var all = (await store.GetAll(get_all, name1, name1))
        .Concat(await store.GetAll(get_all, name2, name2))
        .Concat(await store.GetAll(get_all, name3, name3))
        .Select(se => se with { DatePromoted = se.DateStaged.AddDays(1) });
    await store.Update(all);
    
    await store.DeletePromotedBefore(promoted2, name1, name1);
    
    var se1 = await GetSingleAsSes(get_all, name1, name1);
    var se2 = await GetSingleAsSes(get_all, name2, name2);
    var se3 = await GetSingleAsSes(get_all, name3, name3);
    Assert.That(se1, Is.EqualTo(new StagedEntity(se1.Id, name1, name1, staged2, data2, Hash(data2), promoted2)));
    Assert.That(se2, Is.EqualTo(new StagedEntity(se2.Id, name2, name2, staged2, name2, Hash(name2), promoted2)));
    Assert.That(se3, Is.EqualTo(new StagedEntity(se3.Id, name3, name3, staged2, name3, Hash(name3), promoted2)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1);
    
    var se21 = await GetSingleAsSes(get_all, name2, name2);
    var se22 = await GetSingleAsSes(get_all, name3, name3);
    
    await Assert.ThatAsync(() => GetAsSes(get_all, name1, name1), Is.Empty);
    Assert.That(se21, Is.EqualTo(new StagedEntity(se21.Id, name2, name2, staged2, name2, Hash(name2), promoted2)));
    Assert.That(se22, Is.EqualTo(new StagedEntity(se22.Id, name3, name3, staged2, name3, Hash(name3), promoted2)));
  }
  
  [Test] public async Task Test_stage_single_ignores_duplicates() {
    var (data, stageddt) = (Guid.NewGuid().ToString(), dt.Tick());
    var staged = await store.Stage(stageddt, NAME, NAME, data) ?? throw new Exception();
    var duplicate = await store.Stage(dt.Tick(), NAME, NAME, data);
    
    var expected = new StagedEntity(staged.Id, NAME, NAME, stageddt, data, Hash(data));
    var ses = await GetAsSes(dt.Today, NAME, NAME);
    
    Assert.That(duplicate, Is.Null);
    Assert.That(staged.CloneNew(), Is.EqualTo(expected));
    Assert.That(ses, Is.EquivalentTo(new [] {expected}));
  }
  
  [Test] public async Task Test_staging_multiple_entities_ignores_duplicates() {
    var (start, half) = (dt.Now.AddSeconds(1), LARGE_BATCH_SIZE /  2);
    var staged = (await store.Stage(dt.Tick(), NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => (idx % half).ToString())))
        .Select(e => e.CloneNew()).ToList();
    var staged2 = await GetAsSes(dt.Now.AddYears(-1), NAME, NAME);
    
    Assert.That(staged, Has.Count.EqualTo(half));
    Assert.That(staged, Is.EquivalentTo(staged2));
    Assert.That(staged, Is.EquivalentTo(Enumerable.Range(0, half).Select(idx => new StagedEntity(staged[idx].Id, NAME, NAME, start, idx.ToString(), Hash(idx)))));
  }
  
  [Test] public async Task Test_GetAll_GetUnpromoted_respect_DatePromoted_state() {
    var s1 = await store.Stage(dt.Now, NAME, NAME, "1") ?? throw new Exception();
    var s2 = await store.Stage(dt.Now, NAME, NAME, "2") ?? throw new Exception();
    var s3 = await store.Stage(dt.Now, NAME, NAME, "3") ?? throw new Exception();
    
    var pre = await store.GetAll(dt.Today, NAME, NAME);
    Console.WriteLine("S2: " + s2.Data);
    await store.Update(s2 = s2 with { DatePromoted = dt.Now });
    Console.WriteLine("updateds2: " + s2.Data);
    
    var all = await store.GetAll(dt.Today, NAME, NAME);
    var unpromoted = await store.GetUnpromoted(dt.Today, NAME, NAME);

    Console.WriteLine("pre: " + String.Join(",", pre.Select(e => e.Data)));
    Console.WriteLine("all: " + String.Join(",", all.Select(e => e.Data)));
    Console.WriteLine("unp: " + String.Join(",", unpromoted.Select(e => e.Data)));
    Assert.That(all, Is.EquivalentTo(new [] {s1, s2, s3}));
    Assert.That(unpromoted, Is.EquivalentTo(new [] {s1, s3}));
  }
  
  private string Hash(object o) => TestingFactories.TestingChecksum(o.ToString() ?? throw new Exception());
  
  private async Task<StagedEntity> GetSingleAsSes(DateTime after, SystemName source, ObjectName obj) => 
      (await GetAsSes(after, source, obj)).Single();

  private async Task<List<StagedEntity>> GetAsSes(DateTime after, SystemName source, ObjectName obj) {
    var lst = await store.GetAll(after, source, obj);
    return lst.Select(se => se.CloneNew()).ToList();
  }

}