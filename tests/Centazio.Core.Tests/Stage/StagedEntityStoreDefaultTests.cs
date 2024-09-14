﻿using Centazio.Core.Ctl.Entities;
using Centazio.Core.Helpers;
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
    await store.Stage(NAME, NAME, NAME);
    var fromnow = (await store.GetAll(dt.Now, NAME, NAME)).ToList();
    var minus1 =  await GetSingle(dt.Now.AddMilliseconds(-1), NAME, NAME);
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1, Is.EqualTo((StagedEntity) new StagedEntity.Dto(minus1.Id, NAME, NAME, dt.Now, NAME, Hash(NAME))));
  }
  
  [Test] public async Task Test_updating_single_entity() {
    await store.Stage(NAME, NAME, NAME);
    var created = (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    var updated = created.Promote(dt.Now.AddYears(1));
    await store.Update(updated);
    var updated2 = (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME)).Single();
    
    Assert.That(updated2, Is.EqualTo(updated));
  }

  [Test] public async Task Test_saving_multiple_entities() {
    var staged = (await store.Stage(NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => idx.ToString())) ?? throw new Exception()).
        OrderBy(e => Int32.Parse(e.Data)).
        ToList();
    var fromnow = (await store.GetAll(dt.Now, NAME, NAME)).ToList();
    var minus1 =  (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => (StagedEntity) new StagedEntity.Dto(minus1[idx].Id, NAME, NAME, dt.Now, idx.ToString(), Hash(idx)))));
  }
  
  [Test] public async Task Test_get_returns_in_sorted_order() {
    var ordered = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => dt.Tick()).ToList();
    var random = ordered.OrderBy(_ => Guid.NewGuid()).ToList();
    await random.Select((rand, idx) => {
      using var _ = new ShortLivedUtcDateOverride(rand);
      return store.Stage(NAME, NAME, idx.ToString()) ?? throw new Exception();
    }).Synchronous();
    var retreived = await store.GetAll(TestingDefaults.DefaultStartDt, NAME, NAME);
    var expdates = String.Join(",", ordered);
    var actdates = String.Join(",", retreived.Select(e => e.DateStaged));
    Assert.That(actdates, Is.EqualTo(expdates));
  }

  [Test] public async Task Test_get_returns_in_sorted_order_with_limit() {
    var pgsz = 10;
    store.Limit = pgsz;
    var ordered = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => dt.Tick()).ToList();
    var random = ordered.OrderBy(_ => Guid.NewGuid()).ToList();
    await Task.WhenAll(random.Select((rand, idx) => {
      using var _ = new ShortLivedUtcDateOverride(rand);
      return store.Stage(NAME, NAME, idx.ToString()) ?? throw new Exception();
    }));
    var start = TestingDefaults.DefaultStartDt;
    for (var pgstart = 0; pgstart < LARGE_BATCH_SIZE; pgstart+=pgsz) {
      var page = (await store.GetAll(start, NAME, NAME)).ToList();
      start = page.Last().DateStaged;
      var (actual, exp) = (StrSes(page), StrDts(ordered.Skip(pgstart).Take(pgsz)));
      Assert.That(actual, Is.EqualTo(exp)); 
    }
    
    string StrSes(IEnumerable<StagedEntity> ses) => StrDts(ses.Select(se => se.DateStaged));
    string StrDts(IEnumerable<DateTime> dts) => String.Join(",", dts);
  }
  
  [Test] public async Task Test_updating_multiple_entities() {
    var staged = (await store.Stage(NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => idx.ToString())) ?? throw new Exception())
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var fromnow = (await store.GetAll(dt.Now, NAME, NAME)).ToList();
    var minus1 = (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.GroupBy(e => e.Checksum).Count(), Is.EqualTo(LARGE_BATCH_SIZE), "has duplicate checksums");
    Assert.That(staged, Has.Count.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Has.Count.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => (StagedEntity) new StagedEntity.Dto(minus1[idx].Id, NAME, NAME, dt.Now, idx.ToString(), Hash(idx)))));
  }

  [Test] public async Task Test_saving_multiple_large_entities() {
    var sz = 10000;
    var str = new String('*', sz) + "_";
    
    var staged = (await store.Stage(NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => str + idx)) ?? throw new Exception())
        .Select(e => SetData(e, e.Data.Value.Split('_')[1])) // make it easier to debug without all the noise
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var fromnow = (await store.GetAll(dt.Now, NAME, NAME))
        .Select(e => SetData(e, e.Data.Value.Split('_')[1]))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var minus1 =  (await store.GetAll(dt.Now.AddMilliseconds(-1), NAME, NAME))
        .Select(e => SetData(e, e.Data.Value.Split('_')[1]))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.GroupBy(e => e.Checksum).Count(), Is.EqualTo(LARGE_BATCH_SIZE), "has duplicate checksums");
    Assert.That(staged.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(staged));
    var exp = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => (StagedEntity) new StagedEntity.Dto(minus1[idx].Id, NAME, NAME, dt.Now, str + idx, Hash(str + idx)))
        .Select(e => SetData(e, e.Data.Value.Split('_')[1]))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    Assert.That(minus1, Is.EquivalentTo(exp));
  }
  
  [Test] public async Task Test_get_returns_expected() {
    var (start, staged1) = (dt.Now, dt.Tick());
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    
    await store.Stage(name1, name1, name1);
    var staged2 = dt.Tick();
    await store.Stage(name1, name1, data2);
    await store.Stage(name2, name2, name2);
    await store.Stage(name3, name3, name3);
    
    await Assert.ThatAsync(() => store.GetAll(staged2, name1, name1), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name2, name1), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name1, name2), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name2, name3), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name3, name2), Is.Empty);
    
    var se1_2 = await GetSingle(staged1, name1, name1);
    var ses1 = (await store.GetAll(start, name1, name1)).ToList();
    var ses2 = (await store.GetAll(staged1, name2, name2)).ToList();
    var ses3 = (await store.GetAll(staged1, name3, name3)).ToList();
    Assert.That(ses1.Count(), Is.EqualTo(2));
    Assert.That(se1_2, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se1_2.Id, name1, name1, staged2, data2, Hash(data2))));
    Assert.That(ses2, Is.EquivalentTo(new List<StagedEntity> { (StagedEntity) new StagedEntity.Dto(ses2.Single().Id, name2, name2, staged2, name2, Hash(name2)) }));
    Assert.That(ses3, Is.EquivalentTo(new List<StagedEntity> { (StagedEntity) new StagedEntity.Dto(ses3.Single().Id, name3, name3, staged2, name3, Hash(name3)) }));
  }
  
  [Test] public async Task Test_get_returns_expected_with_ignores() {
    var (start, staged1) = (dt.Now, dt.Tick());
    var (name1, name2, name3) = (NAME + 1, NAME + 2 , NAME + 3);
    
    var notignore = new List<StagedEntity> { await Create(name1, "not ignore: 1.1", "") };
    var staged2 = dt.Tick();
    notignore.Add(await Create(name1, "not ignore: 1.2", " "));
    notignore.Add(await Create(name2, "not ignore: 2", "\r"));
    notignore.Add(await Create(name3, "not ignore: 3", null));
    await store.Update(notignore);
    
    var toignore = new List<StagedEntity> {
      await Create(name1, "ignore: 1.1", "ignore: 1.1"),
      await Create(name1, "ignore: 1.2", "ignore: 1.2"),
      await Create(name2, "ignore: 2", "ignore: 2"),
      await Create(name3, "ignore: 3", "ignore: 3")
    };
    await store.Update(toignore);
    
    await Assert.ThatAsync(() => store.GetAll(staged2, name1, name1), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name2, name1), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name1, name2), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name2, name3), Is.Empty);
    await Assert.ThatAsync(() => store.GetAll(start, name3, name2), Is.Empty);

    var ses1 = await store.GetAll(start, name1, name1);
    var se1_2 = await GetSingle(staged1, name1, name1);
    var ses2 = (await store.GetAll(staged1, name2, name2)).ToList();
    var ses3 = (await store.GetAll(staged1, name3, name3)).ToList();
    Assert.That(se1_2, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se1_2.Id, name1, name1, staged2, "not ignore: 1.2", Hash("not ignore: 1.2"))));
    Assert.That(ses1.Count(), Is.EqualTo(2));
    Assert.That(ses2, Is.EquivalentTo(new List<StagedEntity> { (StagedEntity) new StagedEntity.Dto(ses2.Single().Id, name2, name2, staged2, "not ignore: 2", Hash("not ignore: 2")) }));
    Assert.That(ses3, Is.EquivalentTo(new List<StagedEntity> { (StagedEntity) new StagedEntity.Dto(ses3.Single().Id, name3, name3, staged2, "not ignore: 3", Hash("not ignore: 3")) }));
    
    async Task<StagedEntity> Create(string name, string data, string? ignore) {
      var staged = await store.Stage(name, name, data) ?? throw new Exception();
      return String.IsNullOrWhiteSpace(ignore) ? staged : staged.Ignore(ignore);
    }
  }
  
  [Test] public async Task Test_get_returns_oldest_first_page_as_expected() {
    var pgsz = 10;
    store.Limit = pgsz;
    var start = dt.Now;
    var created = new List<StagedEntity>();
    foreach (var idx in Enumerable.Range(0, 25)) { 
      dt.Tick();
      created.Add(await store.Stage(NAME, NAME, idx.ToString()) ?? throw new Exception());
    }
    
    var exppage1 = created.Take(pgsz).ToList();
    var page1 = (await store.GetAll(start, NAME, NAME)).ToList();
    
    var exppage2 = created.Skip(pgsz).Take(pgsz).ToList();
    var page2 = (await store.GetAll(exppage1.Last().DateStaged, NAME, NAME)).ToList();
    
    var exppage3 = created.Skip(pgsz * 2).Take(pgsz).ToList();
    var page3 = (await store.GetAll(exppage2.Last().DateStaged, NAME, NAME)).ToList();
    
    var page4 = (await store.GetAll(exppage3.Last().DateStaged, NAME, NAME)).ToList();
    
    Assert.That(page1, Is.EquivalentTo(exppage1));
    Assert.That(page2, Is.EquivalentTo(exppage2));
    Assert.That(page3, Is.EquivalentTo(exppage3));
    Assert.That(page4, Is.Empty);
  }
  
  [Test] public async Task Test_delete_staged_before() {
    var (get_all, delete_all) = (dt.Today, dt.Now.AddHours(1));
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    await store.Stage(name1, name1, name1);
    var staged2 = dt.Tick();
    await store.Stage(name1, name1, data2);
    await store.Stage(name2, name2, name2);
    await store.Stage(name3, name3, name3);

    await store.DeleteStagedBefore(staged2, name1, name1); // will delete name1@staged1
    var se1 = await GetSingle(get_all, name1, name1);
    var se2 = await GetSingle(get_all, name2, name2);
    var se3 = await GetSingle(get_all, name3, name3);
    
    Assert.That(se1, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se1.Id, name1, name1, staged2, data2, Hash(data2))));
    Assert.That(se2, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se2.Id, name2, name2, staged2, name2, Hash(name2))));
    Assert.That(se3, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se3.Id, name3, name3, staged2, name3, Hash(name3))));
   
    await store.DeleteStagedBefore(delete_all, name1, name1); // will delete remaining name1
    await Assert.ThatAsync(() => store.GetAll(get_all, name1, name1), Is.Empty);
    var se22 = await GetSingle(get_all, name2, name2);
    var se23 = await GetSingle(get_all, name3, name3);
    Assert.That(se22, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se22.Id, name2, name2, staged2, name2, Hash(name2))));
    Assert.That(se23, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se23.Id, name3, name3, staged2, name3, Hash(name3))));
  }
  
  [Test] public async Task Test_delete_large_batch() {
    await store.Stage(NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => NAME));
    await store.DeleteStagedBefore(dt.Tick(), NAME, NAME); 
    await Assert.ThatAsync(async () => await store.GetAll(dt.Now.AddHours(-1), NAME, NAME), Is.Empty);
  }
    
  [Test] public async Task Test_delete_promoted_before() {
    var (get_all, delete_all) = (dt.Now.AddHours(-1), dt.Now.AddHours(1));
    var (name1, name2, name3, data2) = (NAME + 1, NAME + 2 , NAME + 3, Guid.NewGuid().ToString());
    await store.Stage(name1, name1, name1);
    var (staged2, promoted2) = (dt.Tick(), dt.Now.AddDays(1));
    await store.Stage(name1, name1, data2);
    await store.Stage(name2, name2, name2);
    await store.Stage(name3, name3, name3);
    
    var all = (await store.GetAll(get_all, name1, name1))
        .Concat(await store.GetAll(get_all, name2, name2))
        .Concat(await store.GetAll(get_all, name3, name3))
        .Select(se => se.Promote(se.DateStaged.AddDays(1)));
    await store.Update(all);
    
    await store.DeletePromotedBefore(promoted2, name1, name1);
    
    var se1 = await GetSingle(get_all, name1, name1);
    var se2 = await GetSingle(get_all, name2, name2);
    var se3 = await GetSingle(get_all, name3, name3);
    Assert.That(se1, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se1.Id, name1, name1, staged2, data2, Hash(data2), promoted2)));
    Assert.That(se2, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se2.Id, name2, name2, staged2, name2, Hash(name2), promoted2)));
    Assert.That(se3, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se3.Id, name3, name3, staged2, name3, Hash(name3), promoted2)));
    
    await store.DeleteStagedBefore(delete_all, name1, name1);
    
    var se21 = await GetSingle(get_all, name2, name2);
    var se22 = await GetSingle(get_all, name3, name3);
    
    await Assert.ThatAsync(() => store.GetAll(get_all, name1, name1), Is.Empty);
    Assert.That(se21, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se21.Id, name2, name2, staged2, name2, Hash(name2), promoted2)));
    Assert.That(se22, Is.EqualTo((StagedEntity) new StagedEntity.Dto(se22.Id, name3, name3, staged2, name3, Hash(name3), promoted2)));
  }
  
  [Test] public async Task Test_stage_single_ignores_duplicates() {
    var (data, stageddt) = (Guid.NewGuid().ToString(), dt.Tick());
    var staged = await store.Stage(NAME, NAME, data) ?? throw new Exception();
    dt.Tick();
    var duplicate = await store.Stage(NAME, NAME, data);
    
    var expected = (StagedEntity) new StagedEntity.Dto(staged.Id, NAME, NAME, stageddt, data, Hash(data));
    var ses = (await store.GetAll(dt.Today, NAME, NAME)).ToList();
    
    Assert.That(duplicate, Is.Null);
    Assert.That(staged, Is.EqualTo(expected));
    Assert.That(ses, Is.EquivalentTo(new [] {expected}));
  }
  
  [Test] public async Task Test_staging_multiple_entities_ignores_duplicates() {
    var half = LARGE_BATCH_SIZE;
    var staged = (await store.Stage(NAME, NAME, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => (idx % half).ToString()))).ToList();
    var staged2 = (await store.GetAll(dt.Now.AddYears(-1), NAME, NAME)).ToList();
    
    Assert.That(staged, Has.Count.EqualTo(half));
    Assert.That(staged, Is.EquivalentTo(staged2));
    Assert.That(staged, Is.EquivalentTo(Enumerable.Range(0, half).Select(idx => (StagedEntity) new StagedEntity.Dto(staged[idx].Id, NAME, NAME, dt.Now, idx.ToString(), Hash(idx)))));
  }
  
  [Test] public async Task Test_GetAll_GetUnpromoted_respect_DatePromoted_state() {
    var s1 = await store.Stage(NAME, NAME, "1") ?? throw new Exception();
    var s2 = await store.Stage(NAME, NAME, "2") ?? throw new Exception();
    var s3 = await store.Stage(NAME, NAME, "3") ?? throw new Exception();
    
    await store.Update(s2 = s2.Promote(dt.Now));
    var all = (await store.GetAll(dt.Today, NAME, NAME)).ToList();
    var unpromoted = (await store.GetUnpromoted(dt.Today, NAME, NAME)).ToList();

    Assert.That(all, Is.EquivalentTo(new [] {s1, s2, s3}));
    Assert.That(unpromoted, Is.EquivalentTo(new [] {s1, s3}));
  }
  
  private StagedEntity SetData(StagedEntity e, string data) => (StagedEntity) ((StagedEntity.Dto) e with { Data = data });
  private string Hash(object o) => TestingFactories.TestingChecksum(o.ToString() ?? throw new Exception());
  
  private async Task<StagedEntity> GetSingle(DateTime after, SystemName source, ObjectName obj) => (await store.GetAll(after, source, obj)).Single();

}