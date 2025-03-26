using System.Text.RegularExpressions;
using Centazio.Cli.Commands.Gen.Centazio;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Settings = Centazio.Cli.Commands.Gen.Centazio.GenerateFunctionCommand.Settings;

namespace Centazio.Cli.Tests.Commands;

public class GenerateSlnAndFuncCommandTests {

  private readonly string properroot = FsUtils.GetSolutionRootDirectory();
  private readonly CommandRunner runner = new();
  private readonly string sln = nameof(GenerateSlnAndFuncCommandTests);
  private readonly string slnfile = nameof(GenerateSlnAndFuncCommandTests) + ".sln";
  private readonly string testdir = Path.GetFullPath(Path.Combine(FsUtils.GetSolutionRootDirectory(), "..", "test-generator"));

  [OneTimeSetUp] public void OneTimeSetUp() {
    // todo: remove OneTimeSetUp once generator works
    if (Env.IsGitHubActions()) Assert.Ignore("todo: remove this once generator works");
  }

  [SetUp] public void SetUp() {
    Templater.TestingRootDir = properroot;
    Directory.CreateDirectory(testdir);
    Environment.CurrentDirectory = testdir;

    if (Directory.Exists(sln)) Directory.Delete(sln, true);
  }

  [TearDown] public void TearDown() {
    Templater.TestingRootDir = string.Empty;
  }

  [Test] public async Task Test_generate_solution() {
    var cmd = new GenerateSlnCommand(runner);
    var existsbefore = Directory.Exists(sln);
    await cmd.ExecuteImpl(new GenerateSlnCommand.Settings { SolutionName = sln });

    Assert.That(existsbefore, Is.False);
    Assert.That(Directory.Exists(sln), Is.True);

    Environment.CurrentDirectory = Path.Combine(testdir, sln);
    Assert.That(File.Exists(slnfile), Is.True);
    await ValidateProjectExistsInSln($"{sln}.Shared");

    runner.DotNet("build", Environment.CurrentDirectory);
  }

  [Test] public async Task Test_generate_project() {
    await Test_generate_solution();

    Environment.CurrentDirectory = Path.Combine(testdir, sln);
    var cmd = new GenerateFunctionCommand(runner);

    // todo: add `nameof(Settings.Other)` to `modes` and have corresponding ClickUp function to copy
    var modes = new[] { nameof(Settings.Read), nameof(Settings.Promote), nameof(Settings.Write) };

    await modes.Select(async mode => { await DoMode(mode); }).Synchronous();

    async Task DoMode(string mode) {
      var system = "Test";
      var func = $"{system}{mode}Function";
      var existsbefore = Directory.Exists(func);

      var sett = new Settings { SystemName = "Test" };
      typeof(Settings).GetProperty(mode)!.SetValue(sett, true);
      await cmd.ExecuteImpl(sett);

      Assert.That(existsbefore, Is.False);
      Assert.That(Directory.Exists(func), Is.True);
      Assert.That(File.Exists(Path.Combine(func, $"{func}.csproj")), Is.True);
      Assert.That(File.Exists(Path.Combine(func, $"{func}.cs")), Is.True);
      await ValidateProjectExistsInSln(func);
      runner.DotNet("build", func); // build only the project

      await Directory.GetFiles(func, "*.cs")
          .Select(async file => {
            var contents = await File.ReadAllTextAsync(file);
            Assert.That(contents.Contains("Sample"), Is.False, file);
            Assert.That(contents.Contains("ClickUp"), Is.False, file);
            Assert.That(contents.Contains("AppSheet"), Is.False, file);
            Assert.That(contents.Contains(system), Is.True, file);
          })
          .Synchronous();

      runner.DotNet("build", Environment.CurrentDirectory); // build solution
    }
  }

  [Test] public async Task Test_generate_project_in_existing_assembly() {
    await Test_generate_solution();
    Environment.CurrentDirectory = Path.Combine(testdir, sln);

    var cmd = new GenerateFunctionCommand(runner);
    var sett = new Settings { SystemName = "Test", Read = true };
    await cmd.ExecuteImpl(sett);

    sett = new Settings { SystemName = "Test", AssemblyName = "TestReadFunction", Write = true };
    await cmd.ExecuteImpl(sett);

    Assert.That(Directory.Exists("TestWriteFunction"), Is.False);
    Assert.That(File.Exists(Path.Combine("TestReadFunction", "TestWriteFunction.cs")), Is.True);

    runner.DotNet("build", "TestReadFunction"); // build only the project
    runner.DotNet("build", Environment.CurrentDirectory); // build the whole solution
  }

  private async Task ValidateProjectExistsInSln(string projname) =>
      Assert.That(Regex.Match(await File.ReadAllTextAsync(slnfile),
              @$"Project\(\""\{{.*}}\""\) = \""{projname}\"", \""{projname}\\{projname}.csproj\"", \""\{{.*}}\""")
          .Success);

}