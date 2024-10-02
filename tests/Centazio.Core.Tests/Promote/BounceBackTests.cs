using Centazio.Core.CoreRepo;
using Centazio.Core.Promote;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Promote;

public class BounceBackTests {

  [Test] public async Task Test_any_update_not_from_source_system_is_ignored() {
    
    var toupdate = new List<ICoreEntity> {
      new CoreEntity("1", "1", "1", DateOnly.MinValue, UtcDate.UtcNow),
      new CoreEntity2("3", UtcDate.UtcNow),
      new CoreEntity("2", "2", "2", DateOnly.MinValue, UtcDate.UtcNow),
      new CoreEntity2("4", UtcDate.UtcNow),
    };
    var promoting1 = await toupdate.IgnoreEntitiesBouncingBack(Constants.System1Name);
    var promoting2 = await toupdate.IgnoreEntitiesBouncingBack(Constants.System2Name);
    
    Assert.That(promoting1, Has.Count.EqualTo(2));
    Assert.That(promoting2, Has.Count.EqualTo(2));
    Assert.That(promoting1, Is.EquivalentTo(toupdate.Where(e => e.SourceSystem == Constants.System1Name)));
    Assert.That(promoting2, Is.EquivalentTo(toupdate.Where(e => e.SourceSystem == Constants.System2Name)));
  }

}