using Centazio.Core.Promote;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Promote;

public class BounceBackTests {

  [Test] public void Test_any_update_not_from_source_system_is_ignored() {
    
    var toupdate = new List<Containers.StagedSysCore> {
      new(null!, null!, new CoreEntity(new("1"), "1", "1", DateOnly.MinValue, UtcDate.UtcNow)),
      new(null!, null!, new CoreEntity2(new("3"), UtcDate.UtcNow)),
      new(null!, null!, new CoreEntity(new("2"), "2", "2", DateOnly.MinValue, UtcDate.UtcNow)),
      new(null!, null!, new CoreEntity2(new("4"), UtcDate.UtcNow))
    };
    var promoting1 = PromoteOperationRunner.IgnoreEntitiesBouncingBack(toupdate, Constants.System1Name);
    var promoting2 = PromoteOperationRunner.IgnoreEntitiesBouncingBack(toupdate, Constants.System2Name);
    
    Assert.That(promoting1, Has.Count.EqualTo(2));
    Assert.That(promoting2, Has.Count.EqualTo(2));
    Assert.That(promoting1, Is.EquivalentTo(toupdate.Where(e => e.Core.SourceSystem == Constants.System1Name)));
    Assert.That(promoting2, Is.EquivalentTo(toupdate.Where(e => e.Core.SourceSystem == Constants.System2Name)));
  }
}