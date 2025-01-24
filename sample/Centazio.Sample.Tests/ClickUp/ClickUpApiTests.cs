using Centazio.Sample.ClickUp;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpApiTests {

  [Test] public async Task Test_ClickUpApi_GetTasksAfter() {
    var (settings, secrets) = (F.Settings<SampleSettings>(), F.Secrets<SampleSecrets>());
    var empty = await new ClickUpApi(settings, secrets).GetTasksAfter(DateTime.UtcNow);
    var all = await new ClickUpApi(settings, secrets).GetTasksAfter(DateTime.MinValue.ToUniversalTime());
    var sorted = all.OrderBy(a => a.LastUpdated).ToList();
    
    Assert.That(empty, Is.Empty);
    Assert.That(all, Has.Count.GreaterThan(0));
    Assert.That(all, Is.EqualTo(sorted));
  }

}