namespace Centazio.Core.Tests.Inspect;

public class CheckNullCheckingSemantics {

  [Test] public void Test_null_check_uses_is_operator() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null).ForEach(file => {
      var contents = File.ReadAllText(file);
      if (Regex.IsMatch(contents, @"==\s*null")) errors.Add(file);
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
  
  [Test] public void Test_not_null_check_uses_is_not_operator() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null).ForEach(file => {
      var contents = File.ReadAllText(file);
      if (Regex.IsMatch(contents, @"!=\s*null")) errors.Add(file);
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}