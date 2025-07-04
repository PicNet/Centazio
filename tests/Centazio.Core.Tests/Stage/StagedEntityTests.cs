using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class StagedEntityTests {

  private readonly string MOCK_DATA = Json.Serialize(new {});
  
  [Test] public void Test_initialisation_handles_ignore_correctly() {
    Assert.That(F.TestingStagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignore: null).IgnoreReason, Is.Null);
    Assert.That(F.TestingStagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignore: String.Empty).IgnoreReason, Is.Null);
    Assert.That(F.TestingStagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignore: " ").IgnoreReason, Is.Null);
    Assert.That(F.TestingStagedEntity(Guid.CreateVersion7(), C.System1Name, LifecycleStage.Defaults.Read, UtcDate.UtcNow, MOCK_DATA, Helpers.TestingStagedEntityChecksum(MOCK_DATA), ignore: "\n\t ").IgnoreReason, Is.Null);
  }
}