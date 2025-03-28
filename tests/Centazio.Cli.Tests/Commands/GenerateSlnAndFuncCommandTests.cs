using System.Text.RegularExpressions;
using Centazio.Cli.Commands.Gen.Centazio;
using Centazio.Cli.Infra.Gen;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Settings = Centazio.Cli.Commands.Gen.Centazio.GenerateFunctionCommand.Settings;

namespace Centazio.Cli.Tests.Commands;

public class GenerateSlnAndFuncCommandTests {

  private readonly string properroot = FsUtils.GetSolutionRootDirectory();
  private readonly CommandRunner runner = new();
  private readonly string sln = nameof(GenerateSlnAndFuncCommandTests);
  private readonly string slnfile = nameof(GenerateSlnAndFuncCommandTests) + ".sln";
  private readonly string testdir = Path.GetFullPath(Path.Combine(FsUtils.GetSolutionRootDirectory(), "..", "test-generator"));
  private readonly ICentazioCodeGenerator gen = new CentazioCodeGenerator(new CommandRunner(), new Templater(TestingFactories.Settings(), TestingFactories.Secrets()));
  private readonly string SYSTEM_NAME = "Acme";
  

  [SetUp] public void SetUp() {
    FsUtils.TestingRootDir = properroot;
    Environment.CurrentDirectory = testdir;
    
    FsUtils.EmptyDirectory(testdir);
  }

  [TearDown] public void TearDown() {
    FsUtils.TestingRootDir = string.Empty;
  }

  [Test] public async Task Test_generate_solution() {
    var cmd = new GenerateSlnCommand(gen);
    var existsbefore = Directory.Exists(sln);
    await cmd.ExecuteImpl(new GenerateSlnCommand.Settings { SolutionName = sln });

    Assert.That(existsbefore, Is.False);
    Assert.That(Directory.Exists(sln), Is.True);

    Environment.CurrentDirectory = Path.Combine(testdir, sln);
    Assert.That(File.Exists(slnfile), Is.True);
    await ValidateProjectExistsInSln($"{sln}.Shared");

    runner.DotNet("build", Environment.CurrentDirectory);
  }

  [Test] public async Task Test_generate_project_for_each_lifecycle_stage() {
    await Test_generate_solution();

    Environment.CurrentDirectory = Path.Combine(testdir, sln);
    var cmd = new GenerateFunctionCommand(gen);

    var modes = new[] { nameof(Settings.Read), nameof(Settings.Promote), nameof(Settings.Write), nameof(Settings.Other) };
    await modes.Select(async mode => { await DoMode(mode); }).Synchronous();

    async Task DoMode(string mode) {
      var system = SYSTEM_NAME;
      var func = $"{system}{mode}Function";
      var existsbefore = Directory.Exists(func);

      var sett = new Settings { SystemName = SYSTEM_NAME };
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
            if (!file.EndsWith("Assembly.cs")) Assert.That(contents.Contains(system), Is.True, file);
          })
          .Synchronous();

      runner.DotNet("build", Environment.CurrentDirectory); // build solution
    }
  }

  [Test] public async Task Test_generate_project_in_existing_assembly() {
    await Test_generate_solution();
    Environment.CurrentDirectory = Path.Combine(testdir, sln);

    var cmd = new GenerateFunctionCommand(gen);
    var sett = new Settings { SystemName = SYSTEM_NAME, Read = true };
    await cmd.ExecuteImpl(sett);

    sett = new Settings { SystemName = SYSTEM_NAME, AssemblyName = $"{SYSTEM_NAME}ReadFunction", Write = true };
    await cmd.ExecuteImpl(sett);

    Assert.That(Directory.Exists($"{SYSTEM_NAME}WriteFunction"), Is.False);
    Assert.That(File.Exists(Path.Combine($"{SYSTEM_NAME}ReadFunction", $"{SYSTEM_NAME}WriteFunction.cs")), Is.True);

    runner.DotNet("build", $"{SYSTEM_NAME}ReadFunction"); // build only the project
    runner.DotNet("build", Environment.CurrentDirectory); // build the whole solution
  }

  private async Task ValidateProjectExistsInSln(string projname) =>
      Assert.That(Regex.Match(await File.ReadAllTextAsync(slnfile),
              @$"Project\(\""\{{.*}}\""\) = \""{projname}\"", \""{projname}\\{projname}.csproj\"", \""\{{.*}}\""")
          .Success);

}