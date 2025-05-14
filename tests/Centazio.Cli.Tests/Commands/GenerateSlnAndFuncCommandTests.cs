using System.Text.RegularExpressions;
using Centazio.Cli.Commands.Gen.Centazio;
using Centazio.Cli.Infra.Gen;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Settings = Centazio.Cli.Commands.Gen.Centazio.GenerateFunctionCommand.Settings;

namespace Centazio.Cli.Tests.Commands;

public class GenerateSlnAndFuncCommandTests {

  private readonly string properroot = Environment.CurrentDirectory;
  private readonly CommandRunner runner = new();
  private readonly string sln = nameof(GenerateSlnAndFuncCommandTests);
  private readonly string slnfile = nameof(GenerateSlnAndFuncCommandTests) + ".sln";
  private readonly string testdir = FsUtils.GetCentazioPath("..", "test-generator");
  private readonly string SYSTEM_NAME = "Acme";
  
  private CentazioCodeGenerator nugetgen;
  private CentazioCodeGenerator refgen; 
  

  [SetUp] public async Task SetUp() {
    var settings = await TestingFactories.Settings();
    nugetgen = new(new CommandRunner(), new Templater(settings));
    refgen = new(new CommandRunner(), new Templater(settings), false);
    Environment.SetEnvironmentVariable("IS_CLI", "true");
    FsUtils.TestingCliRootDir = FsUtils.GetCentazioPath();
    Environment.CurrentDirectory = Directory.CreateDirectory(testdir).FullName;
    
    if (Directory.Exists(sln)) Directory.Delete(sln, true);
  }

  [TearDown] public void TearDown() {
    FsUtils.TestingCliRootDir = string.Empty;
    Environment.CurrentDirectory = properroot;
    Environment.SetEnvironmentVariable("IS_CLI", null);
    if (Directory.Exists(testdir)) Directory.Delete(testdir, true);
  }

  [Test] public async Task Test_generate_solution_with_nugets() => 
      await GenerateSlnTestImpl(true);
  
  [Test] public async Task Test_generate_solution_with_references() => 
      await GenerateSlnTestImpl(false);

  // note: funcs generated use the latest Centazio nuget packages, so this test can fail if the
  //    NuGet packages are out of sync with current version of the generated code.
  [Test] public async Task Test_generate_project_for_each_lifecycle_stage_using_nugets() => 
      await GenerateFuncInOwnDirImpl(true);

  [Test] public async Task Test_generate_project_for_each_lifecycle_stage_using_refs() => 
      await GenerateFuncInOwnDirImpl(false);

  // note: funcs generated use the latest Centazio nuget packages, so this test can fail if the
  //    NuGet packages are out of sync with current version of the generated code.
  [Test] public async Task Test_generate_project_in_existing_assembly_using_nugets() => 
      await GenerateProjInExistingAssemblyImpl(true);

  [Test] public async Task Test_generate_project_in_existing_assembly_using_refs() => 
      await GenerateProjInExistingAssemblyImpl(false);
  
  private async Task GenerateSlnTestImpl(bool usenuget) {
    var cmd = new GenerateSlnCommand(GetGen(usenuget));
    var existsbefore = Directory.Exists(sln);
    await cmd.ExecuteImpl(new GenerateSlnCommand.Settings { SolutionName = sln, CoreStorageProvider = "Sqlite" });

    Assert.That(existsbefore, Is.False);
    Assert.That(Directory.Exists(sln), Is.True);

    Environment.CurrentDirectory = Path.Combine(testdir, sln);
    Assert.That(File.Exists(slnfile), Is.True);
    await ValidateProjectExistsInSln($"{sln}.Shared");

    runner.DotNet("build", Environment.CurrentDirectory);
  }
  
  private async Task GenerateFuncInOwnDirImpl(bool usenuget) {
    await GenerateSlnTestImpl(usenuget);
    Environment.CurrentDirectory = Path.Combine(testdir, sln);
    var cmd = new GenerateFunctionCommand(GetGen(usenuget));

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
  
  private async Task GenerateProjInExistingAssemblyImpl(bool usenuget) {
    await GenerateSlnTestImpl(usenuget);
    
    Environment.CurrentDirectory = Path.Combine(testdir, sln);

    var cmd = new GenerateFunctionCommand(GetGen(usenuget));
    var sett = new Settings { SystemName = SYSTEM_NAME, Read = true };
    await cmd.ExecuteImpl(sett);

    sett = new Settings { SystemName = SYSTEM_NAME, AssemblyName = $"{SYSTEM_NAME}ReadFunction", Write = true };
    await cmd.ExecuteImpl(sett);

    Assert.That(Directory.Exists($"{SYSTEM_NAME}WriteFunction"), Is.False);
    Assert.That(File.Exists(Path.Combine($"{SYSTEM_NAME}ReadFunction", $"{SYSTEM_NAME}WriteFunction.cs")), Is.True);

    runner.DotNet("build", $"{SYSTEM_NAME}ReadFunction"); // build only the project
    runner.DotNet("build", Environment.CurrentDirectory); // build the whole solution
  }

  private CentazioCodeGenerator GetGen(bool usenuget) => usenuget ? nugetgen : refgen;
  
  private async Task ValidateProjectExistsInSln(string projname) =>
      Assert.That(Regex.Match(await File.ReadAllTextAsync(slnfile),
              @$"Project\(\""\{{.*}}\""\) = \""{projname}\"", \""{projname}\\{projname}.csproj\"", \""\{{.*}}\""")
          .Success);

}