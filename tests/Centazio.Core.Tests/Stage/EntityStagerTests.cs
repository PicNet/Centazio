using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class EntityStagerTests {
  
  
  private IStagedEntityStore stager;
  
  [SetUp] public void SetUp() {
    stager = new InMemoryStagedEntityStore(100, Helpers.TestingStagedEntityChecksum);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(Constants.System1Name, Constants.SystemEntityName, nameof(EntityStagerTests));
    
    var results1 = (await stager.GetUnpromoted(Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow.AddMilliseconds(-1))).ToList();
    var results2 = (await stager.GetUnpromoted(Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow)).ToList();
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(staged.Id, Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow, nameof(EntityStagerTests), Helpers.TestingStagedEntityChecksum(nameof(EntityStagerTests)))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(Constants.System1Name, Constants.SystemEntityName, datas.ToList());
    
    var results1 = (await stager.GetUnpromoted(Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow.AddMicroseconds(-1))).ToList();
    var results2 = (await stager.GetUnpromoted(Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow)).ToList();
    
    var exp = Enumerable.Range(0, 10).Select(idx => new StagedEntity(results1.ElementAt(idx).Id, Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow, idx.ToString(), Helpers.TestingStagedEntityChecksum(idx.ToString()))).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}