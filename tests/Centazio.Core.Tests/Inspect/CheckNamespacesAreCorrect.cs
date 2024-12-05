using System.Text.RegularExpressions;

namespace Centazio.Core.Tests.Inspect;

public class CheckNamespacesAreCorrect {

  [Test] public void Test_all_namespace_declarations_are_correct() {
    var roots = new [] { "Centazio.Providers", "src", "tests", "sample"};
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CliBootstrapper.cs", "Assembly.cs", "TestSuiteInitialiser.cs", "Properties.cs", "GlobalUsings.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      var m = Regex.Match(contents, "namespace (.*);");
      var ns = m.Groups[1].Value;
      var root = roots.First(r => file.Contains($"{Path.DirectorySeparatorChar}{r}{Path.DirectorySeparatorChar}"));
      var start = file.IndexOf($"{Path.DirectorySeparatorChar}{root}{Path.DirectorySeparatorChar}", StringComparison.Ordinal) + root.Length + 2;
      var exp = String.Join('.', file.Substring(start).Split(Path.DirectorySeparatorChar).SkipLast(1));
      if (ns != exp) errors.Add($"File[{file}] EXP[{exp}] NS[{ns}]");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}