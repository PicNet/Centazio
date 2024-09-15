using System.Text.RegularExpressions;

namespace Centazio.Core.Tests.Inspect;

public class CheckNamespacesAreCorrect {

  [Test] public void Test_all_namespace_declarations_are_correct() {
    var roots = new [] { "Centazio.Providers", "src", "tests"};
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CliBootstrapper.cs", "Assembly.cs", "TestSuiteInitialiser.cs", "Properties.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      var m = Regex.Match(contents, "namespace (.*);");
      var ns = m.Groups[1].Value;
      var root = roots.First(r => file.Contains($"\\{r}\\"));
      var start = file.IndexOf($"\\{root}\\", StringComparison.Ordinal) + root.Length + 2;
      var exp = String.Join('.', file.Substring(start).Split("\\").SkipLast(1));
      if (ns != exp) errors.Add($"File[{file}] EXP[{exp}] NS[{ns}]");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}