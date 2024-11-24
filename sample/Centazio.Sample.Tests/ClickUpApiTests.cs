using System.Text.RegularExpressions;
using Centazio.Core;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests;

public class ClickUpApiTests {

  [Test] public async Task Test_ClickUpApi_GetTasksAfter() {
    var (settings, secrets) = (TestingFactories.Settings(), TestingFactories.Secrets());
    var empty = await new ClickUpApi(settings, secrets).GetTasksAfter(UtcDate.UtcNow);
    var all = await new ClickUpApi(settings, secrets).GetTasksAfter(DateTime.MinValue.ToUniversalTime());
    var page1 = Regex.Matches(all, @"""date_updated"":""([^""]+)""").Select(m => Int64.Parse(m.Groups[1].Value)).ToList();
    var sorted = page1.OrderBy(v => v).ToList();
    
    Assert.That(empty, Is.EqualTo("""{"tasks":[],"last_page":true}"""));
    Assert.That(page1, Has.Count.GreaterThan(0));
    Assert.That(page1, Is.EqualTo(sorted));
  }

}