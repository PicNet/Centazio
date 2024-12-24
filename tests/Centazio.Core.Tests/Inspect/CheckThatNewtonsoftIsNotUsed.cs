namespace Centazio.Core.Tests.Inspect;

public class CheckThatNewtonsoftIsNotUsed {

  [Test] public void Test_no_file_used_Newtonsoft() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CheckThatNewtonsoftIsNotUsed.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      if (contents.Contains("using Newtonsoft")) errors.Add($"File[{file}] appeasr to use Newtonsoft.  Use 'System.Text.Json' instead");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
  
  [Test] public void Test_no_project_references_Newtonsoft() {
    var errors = new List<string>();
    InspectUtils.GetSolnFiles(null, "*.csproj").ForEach(projfn => {
      var contents = File.ReadAllText(projfn);
      if (contents.Contains("Include=\"Newtonsoft.Json\"")) errors.Add($"File[{projfn}] appeasr to use Newtonsoft.  Use 'System.Text.Json' instead");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}