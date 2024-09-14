using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Stage;

public class EntityStagerTests {

  private const string NAME = nameof(EntityStagerTests);
  
  private IStagedEntityStore stager;
  
  [SetUp] public void SetUp() {
    stager = new InMemoryStagedEntityStore(100, TestingFactories.TestingChecksum);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(NAME, NAME, nameof(EntityStagerTests));
    
    var results1 = (await stager.GetUnpromoted(UtcDate.UtcNow.AddMilliseconds(-1), NAME, NAME)).ToList();
    var results2 = (await stager.GetUnpromoted(UtcDate.UtcNow, NAME, NAME)).ToList();
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo((StagedEntity) new StagedEntity.Dto(staged.Id, NAME, NAME, UtcDate.UtcNow, nameof(EntityStagerTests), TestingFactories.TestingChecksum(nameof(EntityStagerTests)))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(NAME, NAME, datas);
    
    var results1 = (await stager.GetUnpromoted(UtcDate.UtcNow.AddMicroseconds(-1), NAME, NAME)).ToList();
    var results2 = (await stager.GetUnpromoted(UtcDate.UtcNow, NAME, NAME)).ToList();
    
    var exp = Enumerable.Range(0, 10).Select(idx => (StagedEntity) new StagedEntity.Dto(results1.ElementAt(idx).Id, NAME, NAME, UtcDate.UtcNow, idx.ToString(), TestingFactories.TestingChecksum(idx.ToString()))).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}