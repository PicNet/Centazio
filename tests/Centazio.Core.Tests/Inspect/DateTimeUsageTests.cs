namespace Centazio.Core.Tests.Inspect;

public class DateTimeUsageTests {

  [Test] public void Test_time_is_only_read_from_an_injected_TimeProvider() {
    // composition roots that choose the real clock, and tests that run against real external systems
    var allowed = new List<string> { 
      "DateTimeUsageTests.cs", "SelfHost.cs", "AzHostCentazioEngineAdapter.cs", "AwsHostCentazioEngineAdapter.cs", 
      "ClickUpApiTests.cs", "AwsEventBridgeChangesNotifierTests.cs" };
    var banned = new List<string> { 
      "DateTime.Now", "DateTime.Today", "DateTime.UtcNow", "DateTimeOffset.Now", "DateTimeOffset.UtcNow", 
      "TimeProvider.System", "Stopwatch", "Environment.TickCount" };
    var errors = FindUsages(InspectUtils.CsFiles(null, allowed), banned);
    Assert.That(errors, Is.Empty, "time must come from the injected `TimeProvider` (`clock.UtcNow()`):\n" + String.Join("\n", errors));
  }
  
  [Test] public void Test_no_waits_or_thread_hops_on_the_core_run_path() {
    var banned = new List<string> { "Task.Delay(", "Thread.Sleep(", "Task.Run(" };
    var errors = FindUsages(InspectUtils.CsFiles(FsUtils.GetCentazioPath("src", "Centazio.Core")), banned);
    Assert.That(errors, Is.Empty, "the function run path must not wait on the wall clock or hop threads, hosts do this:\n" + String.Join("\n", errors));
  }
  
  [Test] public void Test_random_values_and_ids_come_from_an_injected_source() {
    // the production id generator, and cli code that is not on any function's run path
    var allowed = new List<string> { "GuidGenerator.cs", "Zip.cs", "FunctionProjectMeta.cs", "UiTestsCommand.cs" };
    var banned = new List<string> { "new Random()", "Random.Shared", "Guid.NewGuid()", "Guid.CreateVersion7(" };
    var errors = FindUsages(InspectUtils.CsFiles(FsUtils.GetCentazioPath("src"), allowed), banned);
    Assert.That(errors, Is.Empty, "use an injected `IGuidGenerator` or the simulation's seeded random:\n" + String.Join("\n", errors));
  }
  
  [Test] public void Test_DateTimeOffset_from_millis_converts_correctly() {
    var now = F.Clock().UtcNow();
    var dt = UtcDate.FromMillis(new DateTimeOffset(now).ToUnixTimeMilliseconds());
    
    Assert.That(dt.Kind, Is.EqualTo(DateTimeKind.Utc));
    Assert.That(dt, Is.EqualTo(now));
  } 
  
  private List<string> FindUsages(List<string> files, List<string> banned) => files
      .SelectMany(file => {
        var contents = File.ReadAllText(file);
        return banned.Where(b => contents.Contains(b, StringComparison.Ordinal)).Select(b => $"{file} uses {b}");
      })
      .ToList();
}