﻿using Centazio.Core.Entities.Ctl;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace centazio.core.tests.Stage;

public class EntitiesStagerTests {
  private static readonly string NAME = nameof(EntitiesStagerTests);
  
  private TestingUtcDate dt;
  private IStagedEntityStore store;
  private EntitiesStager stager;
  
  [SetUp] public void SetUp() {
    dt = new TestingUtcDate();
    store = new InMemoryStagedEntityStore(100);
    stager = new EntitiesStager(store, dt);
  }
  
  [TearDown] public async Task TearDown() {
    await store.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(NAME, NAME, nameof(EntitiesStagerTests));
    
    var results1 = await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME);
    var results2 = await store.Get(dt.Now, NAME, NAME);
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(NAME, NAME, dt.Now, nameof(EntitiesStagerTests))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(NAME, NAME, datas);
    
    var results1 = await store.Get(dt.Now.AddMicroseconds(-1), NAME, NAME);
    var results2 = await store.Get(dt.Now, NAME, NAME);
    
    var exp = Enumerable.Range(0, 10).Select(i => i.ToString()).Select(idx => new StagedEntity(NAME, NAME, dt.Now, idx.ToString())).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}