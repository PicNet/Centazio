using System.Text.RegularExpressions;
using Centazio.Core;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests;

public class ClickUpApiTests {

  [Test] public async Task Test_ClickUpApi_GetTasksAfter() {
    var (settings, secrets) = (TestingFactories.Settings<SampleSettings, SampleSettings.Dto>(), TestingFactories.Secrets<SampleSecrets, SampleSecrets.Dto>());
    var empty = await new ClickUpApi(settings, secrets).GetTasksAfter(UtcDate.UtcNow);
    var all = await new ClickUpApi(settings, secrets).GetTasksAfter(DateTime.MinValue.ToUniversalTime());
    var page1_dt_updateds = Regex.Matches(all, @"""date_updated"":""([^""]+)""").Select(m => Int64.Parse(m.Groups[1].Value)).ToList();
    var sorted = page1_dt_updateds.OrderBy(v => v).ToList();
    
    Assert.That(empty, Is.EqualTo("""{"tasks":[],"last_page":true}"""));
    Assert.That(page1_dt_updateds, Has.Count.GreaterThan(0));
    Assert.That(page1_dt_updateds, Is.EqualTo(sorted));
  }

}