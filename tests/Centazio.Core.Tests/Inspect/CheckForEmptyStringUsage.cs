namespace Centazio.Core.Tests.Inspect;

public class CheckForEmptyStringUsage {

  [Test] public void Test_no_use_of_empty_string_literal() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CheckForEmptyStringUsage.cs", "SolutionGenerator.cs", "AwsFunctionDeployer.cs").ForEach(file => {
      var contents = File.ReadAllText(file).Replace("\\\"", String.Empty);
      contents = Regex.Replace(contents, "@\".+\"", String.Empty);
      contents = Regex.Replace(contents, "@\\$\".+\"", String.Empty);
      contents = Regex.Replace(contents, "\"\"\".+\"", String.Empty);
      var idx = contents.IndexOf("\"\"", StringComparison.Ordinal); 
      if (idx >= 0)
          errors.Add($"File[{file}] uses '\"\"'. Use String.Empty instead. Ctx[{contents.Substring(Math.Max(0, idx - 10), 20)}]");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}