using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class StagedEntityTests {

  private readonly string MOCK_DATA = Json.Serialize(new {});
  
  [Test] public void Test_initialisation_handles_ignore_correctly() {
    Assert.That(new StagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: null).IgnoreReason, Is.Null);
    Assert.That(new StagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: String.Empty).IgnoreReason, Is.Null);
    Assert.That(new StagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: " ").IgnoreReason, Is.Null);
    Assert.That(new StagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignoreres: "\n\t ").IgnoreReason, Is.Null);
  }
}