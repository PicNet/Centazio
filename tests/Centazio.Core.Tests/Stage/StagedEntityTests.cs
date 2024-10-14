using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class StagedEntityTests {

  private static readonly string NAME = nameof(StagedEntityTests);
  
  [Test] public void Test_initialisation_handles_ignore_correctly() {
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingStagedEntityChecksum(NAME), ignoreres: null).ToBase().IgnoreReason, Is.Null);
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingStagedEntityChecksum(NAME), ignoreres: String.Empty).ToBase().IgnoreReason, Is.Null);
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingStagedEntityChecksum(NAME), ignoreres: " ").ToBase().IgnoreReason, Is.Null);
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingStagedEntityChecksum(NAME), ignoreres: "\n\t ").ToBase().IgnoreReason, Is.Null);
  }
}