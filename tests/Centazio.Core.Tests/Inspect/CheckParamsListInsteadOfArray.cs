namespace Centazio.Core.Tests.Inspect;

public class CheckParamsListInsteadOfArray {

  [Test] public void Test_no_use_of_params_array() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null).ForEach(file => {
      var contents = File.ReadAllText(file);
      if (Regex.Match(contents, @"params \w+\[\]").Success) {
        errors.Add($"File[{file}] uses params array, change to params List<> instead");
      }
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}