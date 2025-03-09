using FsUtils = Centazio.Core.Misc.FsUtils;

namespace Centazio.Core.Tests.Inspect;

public class CheckNamespacesAreCorrect {

  [Test] public void Test_all_namespace_declarations_are_correct() {
    var ignore = new [] { "defaults" };
    var roots = new [] { "Centazio.Providers", "src", "tests", "sample", "generated" };
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CliBootstrapper.cs", "Assembly.cs", "TestSuiteInitialiser.cs", "Properties.cs", "GlobalUsings.cs", "Program.cs").ForEach(file => {
      if (ignore.Any(dir => file.StartsWith(FsUtils.GetSolutionFilePath(dir)))) { return; }
      var root = roots.FirstOrDefault(r => file.Contains($"{Path.DirectorySeparatorChar}{r}{Path.DirectorySeparatorChar}"));
      if (root is null) { throw new Exception($"file[{file}] contains not available roots"); }
      
      var contents = File.ReadAllText(file);
      var m = Regex.Match(contents, "namespace (.*);");
      var ns = m.Groups[1].Value;
      
      var start = file.IndexOf($"{Path.DirectorySeparatorChar}{root}{Path.DirectorySeparatorChar}", StringComparison.Ordinal) + root.Length + 2;
      var exp = String.Join('.', file.Substring(start).Split(Path.DirectorySeparatorChar).SkipLast(1));
      if (ns != exp) errors.Add($"File[{file}] EXP[{exp}] NS[{ns}]");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}