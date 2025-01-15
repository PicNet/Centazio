using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests;

public class ClickUpApiTests {

  [Test] public async Task Test_ClickUpApi_GetTasksAfter() {
    var (settings, secrets) = (TestingFactories.Settings<SampleSettings>(), TestingFactories.Secrets<SampleSecrets>());
    var empty = await new ClickUpApi(settings, secrets).GetTasksAfter(UtcDate.UtcNow);
    var all = await new ClickUpApi(settings, secrets).GetTasksAfter(DateTime.MinValue.ToUniversalTime());
    var sorted = all.OrderBy(a => a.LastUpdated).ToList();
    
    Assert.That(empty, Is.Empty);
    Assert.That(all, Has.Count.GreaterThan(0));
    Assert.That(all, Is.EqualTo(sorted));
  }

}