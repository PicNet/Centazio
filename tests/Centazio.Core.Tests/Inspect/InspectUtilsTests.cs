namespace Centazio.Core.Tests.Inspect;

public class InspectUtilsTests {

  [Test] public void Test_GetSolnFiles_csproj() {
    var csprojs = InspectUtils.GetSolnFiles(null, "*.csproj").Select(f => f.Split(Path.DirectorySeparatorChar).Last()).ToList();
    var exp = new List<string> {
      "Centazio.Cli.csproj",
      "Centazio.Core.csproj",
      "Centazio.Hosts.Self.csproj",
      "Centazio.Test.Lib.csproj",
      "Centazio.Cli.Tests.csproj",
      "Centazio.Core.Tests.csproj",
      "Centazio.Providers.Sqlite.Tests.csproj",
      "Centazio.Providers.Sqlite.csproj",
    };
    Assert.That(exp.All(e => csprojs.Contains(e)), $"Actual:\n\t{String.Join("\n\t", csprojs)}");
  }

}