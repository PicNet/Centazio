using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Stage;

public class EntityStagerTests {
  
  
  private IStagedEntityRepository stager;
  
  [SetUp] public void SetUp() {
    stager = new InMemoryStagedEntityRepository(100, Helpers.TestingStagedEntityChecksum);
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(C.System1Name, C.SystemEntityName, nameof(EntityStagerTests));
    
    var results1 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, UtcDate.UtcNow.AddMilliseconds(-1))).ToList();
    var results2 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, UtcDate.UtcNow)).ToList();
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(staged.Id, C.System1Name, C.SystemEntityName, UtcDate.UtcNow, new(nameof(EntityStagerTests)), Helpers.TestingStagedEntityChecksum(nameof(EntityStagerTests)))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(C.System1Name, C.SystemEntityName, datas.ToList());
    
    var results1 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, UtcDate.UtcNow.AddMicroseconds(-1))).ToList();
    var results2 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, UtcDate.UtcNow)).ToList();
    
    var exp = Enumerable.Range(0, 10).Select(idx => new StagedEntity(results1.ElementAt(idx).Id, C.System1Name, C.SystemEntityName, UtcDate.UtcNow, new(idx.ToString()), Helpers.TestingStagedEntityChecksum(idx.ToString()))).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}