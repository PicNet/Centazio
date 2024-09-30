namespace Centazio.Core.Tests.Inspect;

public class CheckConsoleUsage {

  [Test] public void Test_no_use_of_Console_Write() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "DevelDebug.cs", "CheckConsoleUsage.cs").ForEach(file => {
      var contents = File.ReadAllText(file).Replace("AnsiConsole.Write", "");
      if (contents.IndexOf("Console.Write", StringComparison.Ordinal) >= 0) {
        errors.Add($"File[{file}] uses Console.Write");
      }
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

  [Test] public void Test_no_use_of_Devel_Debug() {
    var errors = new List<string>();
    InspectUtils.CsFiles(null, "CheckConsoleUsage.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      if (contents.IndexOf("DevelDebug.WriteLine", StringComparison.Ordinal) >= 0) {
        errors.Add($"File[{file}] uses Console.Write");
      }
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}