namespace Centazio.Core.Tests.Inspect;

public class DateTimeUsageTests {

  [Test] public void Test_no_DateTime_usage_all_dates_should_be_from_UtcDate() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "DateTimeUsageTests.cs", "UtcDate.cs", "ClickUpApiTests.cs", "function.cs", "E2EEnvironment.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      if (contents.IndexOf("DateTime.Now", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of DateTime.Now, use UtcDate.UtcNow instead"); }
      if (contents.IndexOf("DateTime.Today", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of DateTime.Today, use UtcDate.UtcToday instead"); }
      if (contents.IndexOf("DateTime.UtcNow", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of DateTime.UtcNow, use UtcDate.UtcNow instead"); }
    });
    Assert.That(errors, Is.Empty);
  }
  
  [Test] public void Test_DateTimeOffset_from_millis_converts_correctly() {
    var now = UtcDate.UtcNow;
    var dt = UtcDate.FromMillis(new DateTimeOffset(now).ToUnixTimeMilliseconds());
    
    Assert.That(dt.Kind, Is.EqualTo(DateTimeKind.Utc));
    Assert.That(dt, Is.EqualTo(now));
  } 
}