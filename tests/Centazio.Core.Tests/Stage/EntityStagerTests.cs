using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class EntityStagerTests {

  private const string NAME = nameof(EntityStagerTests);
  
  private IStagedEntityStore stager;
  
  [SetUp] public void SetUp() {
    stager = new InMemoryStagedEntityStore(100, Helpers.TestingStagedEntityChecksum);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(NAME, Constants.ExternalEntityName, nameof(EntityStagerTests));
    
    var results1 = (await stager.GetUnpromoted(UtcDate.UtcNow.AddMilliseconds(-1), NAME, Constants.ExternalEntityName)).ToList();
    var results2 = (await stager.GetUnpromoted(UtcDate.UtcNow, NAME, Constants.ExternalEntityName)).ToList();
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(staged.Id, NAME, Constants.ExternalEntityName, UtcDate.UtcNow, nameof(EntityStagerTests), Helpers.TestingStagedEntityChecksum(nameof(EntityStagerTests)))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(NAME, Constants.ExternalEntityName, datas.ToList());
    
    var results1 = (await stager.GetUnpromoted(UtcDate.UtcNow.AddMicroseconds(-1), NAME, Constants.ExternalEntityName)).ToList();
    var results2 = (await stager.GetUnpromoted(UtcDate.UtcNow, NAME, Constants.ExternalEntityName)).ToList();
    
    var exp = Enumerable.Range(0, 10).Select(idx => new StagedEntity(results1.ElementAt(idx).Id, NAME, Constants.ExternalEntityName, UtcDate.UtcNow, idx.ToString(), Helpers.TestingStagedEntityChecksum(idx.ToString()))).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}