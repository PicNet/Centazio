using centazio.core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace centazio.core.tests.Stage;

public class EntityStagerTests {

  private const string NAME = nameof(EntityStagerTests);
  
  private TestingUtcDate dt;
  private IStagedEntityStore store;
  private EntityStager stager;
  
  [SetUp] public void SetUp() {
    dt = new TestingUtcDate();
    store = new InMemoryStagedEntityStore(100);
    stager = new EntityStager(store);
  }
  
  [TearDown] public async Task TearDown() {
    await store.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(dt.Now, NAME, NAME, nameof(EntityStagerTests));
    
    var results1 = await store.Get(dt.Now.AddMilliseconds(-1), NAME, NAME);
    var results2 = await store.Get(dt.Now, NAME, NAME);
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(NAME, NAME, dt.Now, nameof(EntityStagerTests))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(dt.Now, NAME, NAME, datas);
    
    var results1 = await store.Get(dt.Now.AddMicroseconds(-1), NAME, NAME);
    var results2 = await store.Get(dt.Now, NAME, NAME);
    
    var exp = Enumerable.Range(0, 10).Select(i => i.ToString()).Select(idx => new StagedEntity(NAME, NAME, dt.Now, idx.ToString())).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}