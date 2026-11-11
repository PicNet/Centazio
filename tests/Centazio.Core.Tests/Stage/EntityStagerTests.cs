using Centazio.Core.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Stage;

public class EntityStagerTests {
  
  
  private FakeTimeProvider clock;
  private IStagedEntityRepository stager;
  
  [SetUp] public void SetUp() {
    clock = F.Clock();
    stager = new InMemoryStagedEntityRepository(100, Helpers.TestingStagedEntityChecksum, clock, F.Ids());
  }
  
  [TearDown] public async Task TearDown() {
    await stager.DisposeAsync();
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(C.System1Name, C.SystemEntityName, nameof(EntityStagerTests));
    
    var results1 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, clock.UtcNow().AddMilliseconds(-1))).ToList();
    var results2 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, clock.UtcNow())).ToList();
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(staged.Id, C.System1Name, C.SystemEntityName, clock.UtcNow(), new(nameof(EntityStagerTests)), Helpers.TestingStagedEntityChecksum(nameof(EntityStagerTests)))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(C.System1Name, C.SystemEntityName, datas.ToList());
    
    var results1 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, clock.UtcNow().AddMicroseconds(-1))).ToList();
    var results2 = (await stager.GetUnpromoted(C.System1Name, C.SystemEntityName, clock.UtcNow())).ToList();
    
    var ids = results1.ToDictionary(r => r.Data.Value, r => r.Id);
    var exp = Enumerable.Range(0, 10).Select(idx => new StagedEntity(ids[idx.ToString()], C.System1Name, C.SystemEntityName, clock.UtcNow(), new(idx.ToString()), Helpers.TestingStagedEntityChecksum(idx.ToString()))).ToList();
    Assert.That(results1, Is.EquivalentTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}