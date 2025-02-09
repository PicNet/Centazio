using System.Text.RegularExpressions;

namespace Centazio.Core.Tests.Inspect;

public class CheckNoConfusingFuncUsage {

  [Test] public void Test_no_use_of_complex_Func() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "GlobalEnumerableExtensionMethods.cs", "AbstractCtlRepository.cs", "CentazioServicesRegistrar.cs").ForEach(file => {
      if (file.EndsWith("Tests.cs", StringComparison.Ordinal)) return;
      var contents = File.ReadAllText(file);
      var matches = Regex.Matches(contents, "Func<([^>]+)>", RegexOptions.Singleline)
          .Select(m => m.Groups[1].Value)
          .Where(v => v.IndexOf("Checksum", StringComparison.Ordinal) < 0)
          .Where(v => v.Split(',').Length > 1);
      errors.AddRange(matches.Select(m => $"File[{file}]: Func<{m}>"));
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}