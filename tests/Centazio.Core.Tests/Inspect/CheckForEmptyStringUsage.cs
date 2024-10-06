using System.Text.RegularExpressions;

namespace Centazio.Core.Tests.Inspect;

public class CheckForEmptyStringUsage {

  [Test] public void Test_no_use_of_empty_string_literal() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CheckForEmptyStringUsage.cs").ForEach(file => {
      var contents = File.ReadAllText(file).Replace("\\\"", String.Empty);
      contents = Regex.Replace(contents, "@\".*", "");
      if (contents.IndexOf("\"\"", StringComparison.Ordinal) >= 0)
          errors.Add($"File[{file}] uses '\"\"'. Use String.Empty instead");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}