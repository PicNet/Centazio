namespace Centazio.Core.Tests.Inspect;

public class CheckEnumerableUsageStandards {

  private readonly string[] NOT_ALLOWED = ["IEnumerable", "ICollection", "IList", "IDictionary"];

  [Test] public void Test_no_use_of_IEnumerable() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "GlobalEnumerableExtensionMethods.cs", "CheckEnumerableUsageStandards.cs", "EpochTracker.cs", "Rng.cs", "E2EEnvironment.cs")
        .ForEach(file => {
          var contents = File.ReadAllText(file);
          NOT_ALLOWED.ForEach(na => {
            if (contents.IndexOf($"{na}<", StringComparison.Ordinal) >= 0) errors.Add($"file[{file}] uses {na}<>.  Use List<> or Dictionary<> instead to avoid confusions.");
          });
        });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}