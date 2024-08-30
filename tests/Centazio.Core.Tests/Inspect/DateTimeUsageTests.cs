namespace Centazio.Core.Tests.Inspect;

public class DateTimeUsageTests {

  [Test] public void Test_no_DateTime_usage_all_dates_should_be_from_UtcDate() {
    var errors = new List<string>();
    InspectUtils.CsFiles("DateTimeUsageTests.cs", "UtcDate.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      if (contents.IndexOf("DateTime.Now", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of DateTime.Now, use UtcDate.UtcNow instead"); }
      if (contents.IndexOf("DateTime.Today", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of DateTime.Today, use UtcDate.UtcToday instead"); }
      if (contents.IndexOf("DateTime.UtcNow", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of DateTime.UtcNow, use UtcDate.UtcNow instead"); }
      if (contents.IndexOf("UtcDate.Now.Now", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of UtcDate.Now.Now, use UtcDate.UtcNow instead"); }
      if (contents.IndexOf("UtcDate.Now.UtcNow", StringComparison.Ordinal) >= 0) { errors.Add($"{file} has invalid usage of UtcDate.Now.UtcNow, use UtcDate.UtcNow instead"); }
    });
    Assert.That(errors, Is.Empty);
  }
}