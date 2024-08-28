using Centazio.Core;
using centazio.core.Ctl.Entities;
using Centazio.Core.Stage;

namespace centazio.core.tests.Stage;

public class EntityStagerTests {

  private const string NAME = nameof(EntityStagerTests);
  
  private IStagedEntityStore stager;
  
  [SetUp] public void SetUp() {
    stager = new InMemoryStagedEntityStore(100);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(UtcDate.Utc.Now, NAME, NAME, nameof(EntityStagerTests));
    
    var results1 = await stager.Get(UtcDate.Utc.Now.AddMilliseconds(-1), NAME, NAME);
    var results2 = await stager.Get(UtcDate.Utc.Now, NAME, NAME);
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(NAME, NAME, UtcDate.Utc.Now, nameof(EntityStagerTests))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(UtcDate.Utc.Now, NAME, NAME, datas);
    
    var results1 = await stager.Get(UtcDate.Utc.Now.AddMicroseconds(-1), NAME, NAME);
    var results2 = await stager.Get(UtcDate.Utc.Now, NAME, NAME);
    
    var exp = Enumerable.Range(0, 10).Select(i => i.ToString()).Select(idx => new StagedEntity(NAME, NAME, UtcDate.Utc.Now, idx.ToString())).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}