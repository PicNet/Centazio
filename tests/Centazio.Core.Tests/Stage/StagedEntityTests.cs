using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class StagedEntityTests {

  private readonly string MOCK_DATA = Json.Serialize(new {});
  
  [Test] public void Test_initialisation_handles_ignore_correctly() {
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: null).ToBase().IgnoreReason, Is.Null);
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: String.Empty).ToBase().IgnoreReason, Is.Null);
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: " ").ToBase().IgnoreReason, Is.Null);
    Assert.That(new StagedEntity.Dto(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: "\n\t ").ToBase().IgnoreReason, Is.Null);
  }
}