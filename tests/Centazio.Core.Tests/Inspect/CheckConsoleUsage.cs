namespace Centazio.Core.Tests.Inspect;

public class CheckConsoleUsage {

  [Test] public void Test_no_use_of_Console_Write() {
    var errors = new List<string>();
    var NOT_ALLOWED = new [] { "Console.Write", "DevelDebug.Write" };
    InspectUtils.CsFiles(null, "DevelDebug.cs", "DevelDebugTests.cs", "CheckConsoleUsage.cs", "SimulationCtx.cs", "CentazioHost.cs", "ClickUpApiTests.cs", "CommandRunner.cs").ForEach(file => {
      var contents = File.ReadAllText(file).Replace("AnsiConsole.Write", String.Empty);
      NOT_ALLOWED.ForEach(na => {
        if (contents.IndexOf(na, StringComparison.Ordinal) >= 0) {
          errors.Add($"File[{file}] uses '{na}'");
        }
      });
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
  
  [Test] public void Test_no_use_of_AnsiConsole_in_CLI_project() {
    var errors = new List<string>();
    var NOT_ALLOWED = new [] { "AnsiConsole." };
    InspectUtils.CsFiles(FsUtils.GetSolutionFilePath("src", "Centazio.Cli"), "Cli.cs", "UiHelpers.cs", "UiTestsCommand.cs").ForEach(file => {
      var contents = File.ReadAllText(file);
      NOT_ALLOWED.ForEach(na => {
        if (contents.IndexOf(na, StringComparison.Ordinal) >= 0) {
          errors.Add($"File[{file}] uses '{na}'.  Replace with helper methods in UiHelpers.cs");
        }
      });
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}