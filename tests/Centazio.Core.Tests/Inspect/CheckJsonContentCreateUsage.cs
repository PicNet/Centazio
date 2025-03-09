namespace Centazio.Core.Tests.Inspect;

public class CheckJsonContentCreateUsage {

  [Test] public void Test_no_use_of_JsonContent_Create() {
    var errors = new List<string>();
    var NOT_ALLOWED = new [] { "JsonContent.Create", "new StringContent" };
    InspectUtils.CsFiles(null, "CheckJsonContentCreateUsage.cs", "Json.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      NOT_ALLOWED.ForEach(na => {
        if (contents.IndexOf(na, StringComparison.Ordinal) >= 0) {
          errors.Add($"File[{file}] uses '{na}'.  Should instead use `Json.SerializeToHttpContent(...)`");
        }
      });
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}