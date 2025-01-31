namespace Centazio.Core.Tests.Inspect;

public class CheckJsonSerializerIsNotUsed {

  [Test] public void Test_no_use_of_default_serialiser() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "Json.cs", "JsonTests.cs", "CheckJsonSerializerIsNotUsed.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      if (contents.IndexOf("JsonSerializer.", StringComparison.Ordinal) >= 0)
          errors.Add($"File[{file}] uses `JsonSerializer`. Use `Json` instead");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}