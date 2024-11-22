using Centazio.Core;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests;

public class ClickUpApiTests {

  [Test] public async Task Test_ClickUpApi_GetTasksAfter() {
    var (settings, secrets) = (TestingFactories.Settings(), TestingFactories.Secrets());
    var tasks = await new ClickUpApi(settings, secrets).GetTasksAfter(UtcDate.UtcNow);
    Assert.That(tasks, Is.Empty);
  }

}