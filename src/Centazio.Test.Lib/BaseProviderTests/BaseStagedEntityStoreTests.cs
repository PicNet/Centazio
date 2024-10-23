using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class StagedEntityRepositoryDefaultTests {

  private const int LARGE_BATCH_SIZE = 100;
  private readonly string MOCK_DATA = Json.Serialize(new {});
  
  private IStagedEntityRepository repo = null!;
  private TestingUtcDate dt = null!;
  
  protected abstract Task<IStagedEntityRepository> GetRepository(int limit=0, Func<string, StagedEntityChecksum>? checksum = null);
  
  [SetUp] public async Task SetUp() {
    repo = await GetRepository(0, Hash);
    dt = (TestingUtcDate) UtcDate.Utc;
  }

  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public async Task Test_saving_single_entity() {
    await repo.Stage(Constants.System1Name, Constants.SystemEntityName, MOCK_DATA);
    var fromnow = await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now);
    var minus1 =  await GetSingle(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddMilliseconds(-1));
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(minus1, Is.EqualTo(new StagedEntity(minus1.Id, Constants.System1Name, Constants.SystemEntityName, dt.Now, MOCK_DATA, Hash(MOCK_DATA))));
  }
  
  [Test] public async Task Test_updating_single_entity() {
    await repo.Stage(Constants.System1Name, Constants.SystemEntityName, MOCK_DATA);
    var created = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddMilliseconds(-1))).Single();
    var updated = created.Promote(dt.Now.AddYears(1));
    await repo.Update(updated);
    var updated2 = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddMilliseconds(-1))).Single();
    
    Assert.That(updated2, Is.EqualTo(updated));
  }

  [Test] public async Task Test_saving_multiple_entities() {
    var staged = (await repo.Stage(Constants.System1Name, Constants.SystemEntityName, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => idx.ToString()).ToList()) ?? throw new Exception()).
        OrderBy(e => Int32.Parse(e.Data)).
        ToList();
    var fromnow = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now)).ToList();
    var minus1 =  (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddMilliseconds(-1)))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(minus1[idx].Id, Constants.System1Name, Constants.SystemEntityName, dt.Now, idx.ToString(), Hash(idx)))));
  }
  
  [Test] public async Task Test_get_returns_in_sorted_order() {
    var ordered = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => dt.Tick()).ToList();
    var random = ordered.OrderBy(_ => Guid.NewGuid()).ToList();
    await random.Select((rand, idx) => {
      using var _ = new ShortLivedUtcDateOverride(rand);
      return repo.Stage(Constants.System1Name, Constants.SystemEntityName, idx.ToString()) ?? throw new Exception();
    }).Synchronous();
    var retreived = await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, TestingDefaults.DefaultStartDt);
    var expdates = String.Join(",", ordered);
    var actdates = String.Join(",", retreived.Select(e => e.DateStaged));
    Assert.That(actdates, Is.EqualTo(expdates));
  }

  [Test] public async Task Test_get_returns_in_sorted_order_with_limit() {
    var pgsz = 10;
    repo.Limit = pgsz;
    var ordered = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => dt.Tick()).ToList();
    var random = ordered.OrderBy(_ => Guid.NewGuid()).ToList();
    await random.Select((rand, idx) => {
      using var _ = new ShortLivedUtcDateOverride(rand);
      return repo.Stage(Constants.System1Name, Constants.SystemEntityName, idx.ToString()) ?? throw new Exception();
    }).Synchronous();
    var start = TestingDefaults.DefaultStartDt;
    for (var pgstart = 0; pgstart < LARGE_BATCH_SIZE; pgstart+=pgsz) {
      var page = await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, start);
      start = page.Last().DateStaged;
      var (actual, exp) = (StrSes(page), StrDts(ordered.Skip(pgstart).Take(pgsz).ToList()));
      Assert.That(actual, Is.EqualTo(exp)); 
    }
    
    string StrSes(List<StagedEntity> ses) => StrDts(ses.Select(se => se.DateStaged).ToList());
    string StrDts(List<DateTime> dts) => String.Join(",", dts);
  }
  
  [Test] public async Task Test_updating_multiple_entities() {
    var staged = (await repo.Stage(Constants.System1Name, Constants.SystemEntityName, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => idx.ToString()).ToList()) ?? throw new Exception())
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var fromnow = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now)).ToList();
    var minus1 = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddMilliseconds(-1)))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.GroupBy(e => e.StagedEntityChecksum).Count(), Is.EqualTo(LARGE_BATCH_SIZE), "has duplicate checksums");
    Assert.That(staged, Has.Count.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Has.Count.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(minus1[idx].Id, Constants.System1Name, Constants.SystemEntityName, dt.Now, idx.ToString(), Hash(idx)))));
  }

  [Test] public async Task Test_saving_multiple_large_entities() {
    var sz = 10000;
    var str = new String('*', sz) + "_";
    
    var staged = (await repo.Stage(Constants.System1Name, Constants.SystemEntityName, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => str + idx).ToList()) ?? throw new Exception())
        .Select(e => SetData(e, e.Data.Value.Split('_')[1])) // make it easier to debug without all the noise
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var fromnow = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now))
        .Select(e => SetData(e, e.Data.Value.Split('_')[1]))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    var minus1 =  (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddMilliseconds(-1)))
        .Select(e => SetData(e, e.Data.Value.Split('_')[1]))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    
    Assert.That(fromnow, Is.Empty);
    Assert.That(staged.GroupBy(e => e.StagedEntityChecksum).Count(), Is.EqualTo(LARGE_BATCH_SIZE), "has duplicate checksums");
    Assert.That(staged.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1.Count, Is.EqualTo(LARGE_BATCH_SIZE));
    Assert.That(minus1, Is.EquivalentTo(staged));
    var exp = Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => new StagedEntity(minus1[idx].Id, Constants.System1Name, Constants.SystemEntityName, dt.Now, str + idx, Hash(str + idx)))
        .Select(e => SetData(e, e.Data.Value.Split('_')[1]))
        .OrderBy(e => Int32.Parse(e.Data))
        .ToList();
    Assert.That(minus1, Is.EquivalentTo(exp));
  }
  
  [Test] public async Task Test_get_returns_expected() {
    var (start, staged1) = (dt.Now, dt.Tick());
    var basenm = Constants.System1Name;
    var (name1, name2, name3, data2) = (basenm + 1, basenm + 2 , basenm + 3, Guid.NewGuid().ToString());
    
    await repo.Stage(name1, new(name1), name1);
    var staged2 = dt.Tick();
    await repo.Stage(name1, new(name1), data2);
    await repo.Stage(name2, new(name2), name2);
    await repo.Stage(name3, new(name3), name3);
    
    await Assert.ThatAsync(() => repo.GetAll(name1, new(name1), staged2), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name2, new(name1), start), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name1, new(name2), start), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name2, new(name3), start), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name3, new(name2), start), Is.Empty);
    
    var se1_2 = await GetSingle(name1, new(name1), staged1);
    var ses1 = await repo.GetAll(name1, new(name1), start);
    var ses2 = await repo.GetAll(name2, new(name2), staged1);
    var ses3 = await repo.GetAll(name3, new(name3), staged1);
    Assert.That(ses1.Count, Is.EqualTo(2));
    Assert.That(se1_2, Is.EqualTo(new StagedEntity(se1_2.Id, name1, new(name1), staged2, data2, Hash(data2))));
    Assert.That(ses2, Is.EquivalentTo(new List<StagedEntity> { new(ses2.Single().Id, name2, new(name2), staged2, name2, Hash(name2)) }));
    Assert.That(ses3, Is.EquivalentTo(new List<StagedEntity> { new(ses3.Single().Id, name3, new(name3), staged2, name3, Hash(name3)) }));
  }
  
  [Test] public async Task Test_single_ignore_update() {
    var staged = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, nameof(StagedEntity.Data)) ?? throw new Exception();
    await repo.Update(Constants.System1Name, Constants.SystemEntityName, [staged with { IgnoreReason = nameof(StagedEntity.IgnoreReason) }]);
    var all = await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, DateTime.MinValue);
    Assert.That(all, Is.Empty);
  }
  
  [Test] public async Task Test_get_returns_expected_with_ignores() {
    var (start, staged1) = (dt.Now, dt.Tick());
    var name = Constants.System1Name.Value;
    var (name1, name2, name3) = (name + 1, name + 2 , name + 3);
    
    await Create(name1, "not ignore: 1.1", String.Empty);
    var staged2 = dt.Tick();
    await Create(name1, "not ignore: 1.2", " ");
    await Create(name2, "not ignore: 2", "\r");
    await Create(name3, "not ignore: 3", null);
    
    await repo.Update(name1, new(name1), [
      await Create(name1, "ignore: 1.1", "ignore: 1.1"),
      await Create(name1, "ignore: 1.2", "ignore: 1.2")
    ]);
    await repo.Update(name2, new(name2), [
      await Create(name2, "ignore: 2", "ignore: 2"),
    ]);
    await repo.Update(name3, new(name3), [
      await Create(name3, "ignore: 3", "ignore: 3")
    ]);
    var all = await repo.GetAll(name2, new(name2), start);
    Console.WriteLine("IN DB: " + Json.Serialize(all));
    
    
    await Assert.ThatAsync(() => repo.GetAll(name1, new(name1), staged2), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name2, new(name1), start), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name1, new(name2), start), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name2, new(name3), start), Is.Empty);
    await Assert.ThatAsync(() => repo.GetAll(name3, new(name2), start), Is.Empty);

    var ses1 = await repo.GetAll(name1, new(name1), start);
    var se1_2 = await GetSingle(name1, new(name1), staged1);
    var ses2 = (await repo.GetAll(name2, new(name2), staged1)).ToList();
    var ses3 = (await repo.GetAll(name3, new(name3), staged1)).ToList();
    Assert.That(se1_2, Is.EqualTo(new StagedEntity(se1_2.Id, name1, new(name1), staged2, "not ignore: 1.2", Hash("not ignore: 1.2"))));
    Assert.That(ses1.Count, Is.EqualTo(2));
    Assert.That(ses2, Is.EquivalentTo(new List<StagedEntity> { new(ses2.Single().Id, name2, new(name2), staged2, "not ignore: 2", Hash("not ignore: 2")) }));
    Assert.That(ses3, Is.EquivalentTo(new List<StagedEntity> { new(ses3.Single().Id, name3, new(name3), staged2, "not ignore: 3", Hash("not ignore: 3")) }));
    
    async Task<StagedEntity> Create(string nm, string data, string? ignore) {
      var staged = await repo.Stage(nm, new(nm), data) ?? throw new Exception();
      return String.IsNullOrWhiteSpace(ignore) ? staged : staged.Ignore(ignore);
    }
  }
  
  [Test] public async Task Test_get_returns_oldest_first_page_as_expected() {
    var pgsz = 10;
    repo.Limit = pgsz;
    var start = dt.Now;
    var created = new List<StagedEntity>();
    foreach (var idx in Enumerable.Range(0, 25)) { 
      dt.Tick();
      created.Add(await repo.Stage(Constants.System1Name, Constants.SystemEntityName, idx.ToString()) ?? throw new Exception());
    }
    
    var exppage1 = created.Take(pgsz).ToList();
    var page1 = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, start)).ToList();
    
    var exppage2 = created.Skip(pgsz).Take(pgsz).ToList();
    var page2 = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, exppage1.Last().DateStaged)).ToList();
    
    var exppage3 = created.Skip(pgsz * 2).Take(pgsz).ToList();
    var page3 = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, exppage2.Last().DateStaged)).ToList();
    
    var page4 = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, exppage3.Last().DateStaged)).ToList();
    
    Assert.That(page1, Is.EquivalentTo(exppage1));
    Assert.That(page2, Is.EquivalentTo(exppage2));
    Assert.That(page3, Is.EquivalentTo(exppage3));
    Assert.That(page4, Is.Empty);
  }
  
  [Test] public async Task Test_delete_staged_before() {
    var (get_all, delete_all) = (dt.Today, dt.Now.AddHours(1));
    var basenm = Constants.System1Name.Value;
    var (name1, name2, name3, data2) = (basenm + 1, basenm + 2 , basenm + 3, Guid.NewGuid().ToString());
    await repo.Stage(name1, new(name1), name1);
    var staged2 = dt.Tick();
    await repo.Stage(name1, new(name1), data2);
    await repo.Stage(name2, new(name2), name2);
    await repo.Stage(name3, new(name3), name3);

    await repo.DeleteStagedBefore(name1, new(name1), staged2); // will delete name1@staged1
    var se1 = await GetSingle(name1, new(name1), get_all);
    var se2 = await GetSingle(name2, new(name2), get_all);
    var se3 = await GetSingle(name3, new(name3), get_all);
    
    Assert.That(se1, Is.EqualTo(new StagedEntity(se1.Id, name1, new(name1), staged2, data2, Hash(data2))));
    Assert.That(se2, Is.EqualTo(new StagedEntity(se2.Id, name2, new(name2), staged2, name2, Hash(name2))));
    Assert.That(se3, Is.EqualTo(new StagedEntity(se3.Id, name3, new(name3), staged2, name3, Hash(name3))));
   
    await repo.DeleteStagedBefore(name1, new(name1), delete_all); // will delete remaining name1
    await Assert.ThatAsync(() => repo.GetAll(name1, new(name1), get_all), Is.Empty);
    var se22 = await GetSingle(name2, new(name2), get_all);
    var se23 = await GetSingle(name3, new(name3), get_all);
    Assert.That(se22, Is.EqualTo(new StagedEntity(se22.Id, name2, new(name2), staged2, name2, Hash(name2))));
    Assert.That(se23, Is.EqualTo(new StagedEntity(se23.Id, name3, new(name3), staged2, name3, Hash(name3))));
  }
  
  [Test] public async Task Test_delete_large_batch() {
    await repo.Stage(Constants.System1Name, Constants.SystemEntityName, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(_ => MOCK_DATA).ToList());
    await repo.DeleteStagedBefore(Constants.System1Name, Constants.SystemEntityName, dt.Tick()); 
    await Assert.ThatAsync(async () => await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddHours(-1)), Is.Empty);
  }
    
  [Test] public async Task Test_delete_promoted_before() {
    var (get_all, delete_all) = (dt.Now.AddHours(-1), dt.Now.AddHours(1));
    var basenm = Constants.System1Name;
    var (name1, name2, name3, data2) = (basenm + 1, basenm + 2 , basenm + 3, Guid.NewGuid().ToString());
    await repo.Stage(name1, new(name1), name1);
    var (staged2, promoted2) = (dt.Tick(), dt.Now.AddDays(1));
    await repo.Stage(name1, new(name1), data2);
    await repo.Stage(name2, new(name2), name2);
    await repo.Stage(name3, new(name3), name3);
    
    await repo.Update(name1, new(name1), (await repo.GetAll(name1, new(name1), get_all)).Select(se => se.Promote(se.DateStaged.AddDays(1))).ToList());
    await repo.Update(name2, new(name2), (await repo.GetAll(name2, new(name2), get_all)).Select(se => se.Promote(se.DateStaged.AddDays(1))).ToList());
    await repo.Update(name3, new(name3), (await repo.GetAll(name3, new(name3), get_all)).Select(se => se.Promote(se.DateStaged.AddDays(1))).ToList());
    
    await repo.DeletePromotedBefore(name1, new(name1), promoted2);
    
    var se1 = await GetSingle(name1, new(name1), get_all);
    var se2 = await GetSingle(name2, new(name2), get_all);
    var se3 = await GetSingle(name3, new(name3), get_all);
    Assert.That(se1, Is.EqualTo(new StagedEntity(se1.Id, name1, new(name1), staged2, data2, Hash(data2)) { DatePromoted = promoted2 }));
    Assert.That(se2, Is.EqualTo(new StagedEntity(se2.Id, name2, new(name2), staged2, name2, Hash(name2)) { DatePromoted = promoted2 }));
    Assert.That(se3, Is.EqualTo(new StagedEntity(se3.Id, name3, new(name3), staged2, name3, Hash(name3)) { DatePromoted = promoted2 }));
    
    await repo.DeleteStagedBefore(name1, new(name1), delete_all);
    
    var se21 = await GetSingle(name2, new(name2), get_all);
    var se22 = await GetSingle(name3, new(name3), get_all);
    
    await Assert.ThatAsync(() => repo.GetAll(name1, new(name1), get_all), Is.Empty);
    Assert.That(se21, Is.EqualTo(new StagedEntity(se21.Id, name2, new(name2), staged2, name2, Hash(name2)) { DatePromoted = promoted2 }));
    Assert.That(se22, Is.EqualTo(new StagedEntity(se22.Id, name3, new(name3), staged2, name3, Hash(name3)) { DatePromoted = promoted2 }));
  }
  
  [Test] public async Task Test_stage_single_ignores_duplicates() {
    var (data, stageddt) = (Guid.NewGuid().ToString(), dt.Tick());
    var staged = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, data) ?? throw new Exception();
    dt.Tick();
    var duplicate = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, data);
    
    var expected = new StagedEntity(staged.Id, Constants.System1Name, Constants.SystemEntityName, stageddt, data, Hash(data));
    var ses = (await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Today)).ToList();
    
    Assert.That(duplicate, Is.Null);
    Assert.That(staged, Is.EqualTo(expected));
    Assert.That(ses, Is.EquivalentTo(new [] {expected}));
  }
  
  [Test] public async Task Test_staging_multiple_entities_ignores_duplicates() {
    var half = LARGE_BATCH_SIZE;
    var staged = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, Enumerable.Range(0, LARGE_BATCH_SIZE).Select(idx => (idx % half).ToString()).ToList());
    var staged2 = await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Now.AddYears(-1));
    
    Assert.That(staged, Has.Count.EqualTo(half));
    Assert.That(staged, Is.EquivalentTo(staged2));
    Assert.That(staged, Is.EquivalentTo(Enumerable.Range(0, half).Select(idx => new StagedEntity(staged[idx].Id, Constants.System1Name, Constants.SystemEntityName, dt.Now, idx.ToString(), Hash(idx)))));
  }
  
  [Test] public async Task Test_GetAll_GetUnpromoted_respect_DatePromoted_state() {
    var s1 = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, "1") ?? throw new Exception();
    var s2 = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, "2") ?? throw new Exception();
    var s3 = await repo.Stage(Constants.System1Name, Constants.SystemEntityName, "3") ?? throw new Exception();
    
    await repo.Update(s2 = s2.Promote(dt.Now));
    var all = await repo.GetAll(Constants.System1Name, Constants.SystemEntityName, dt.Today);
    var unpromoted = await repo.GetUnpromoted(Constants.System1Name, Constants.SystemEntityName, dt.Today);

    Assert.That(all, Is.EquivalentTo(new [] {s1, s2, s3}));
    Assert.That(unpromoted, Is.EquivalentTo(new [] {s1, s3}));
  }
  
  private StagedEntity SetData(StagedEntity e, string data) => e with { Data = data };
  private StagedEntityChecksum Hash(object o) => Helpers.TestingStagedEntityChecksum(o.ToString() ?? throw new Exception());
  
  private async Task<StagedEntity> GetSingle(SystemName system, SystemEntityTypeName systype, DateTime after) => (await repo.GetAll(system, systype, after)).Single();

}