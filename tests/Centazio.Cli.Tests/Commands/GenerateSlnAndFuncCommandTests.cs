using System.Text.RegularExpressions;
using Centazio.Cli.Commands.Gen.Centazio;
using Centazio.Cli.Infra.Gen;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Settings = Centazio.Cli.Commands.Gen.Centazio.GenerateFunctionCommand.Settings;

namespace Centazio.Cli.Tests.Commands;

/// <summary>
/// The 'nugets' cases build the generated code against Centazio packages packed from this checkout into a local
/// feed (see `CreateLocalPackageFeed`), so the templates and the packages always match and nothing needs to be
/// published first.  The 'refs' cases use project references.
/// </summary>
public class GenerateSlnAndFuncCommandTests {

  private readonly string properroot = Environment.CurrentDirectory;
  private readonly CommandRunner runner = new();
  private readonly string sln = nameof(GenerateSlnAndFuncCommandTests);
  private readonly string slnfile = nameof(GenerateSlnAndFuncCommandTests) + ".sln";
  private readonly string testdir = FsUtils.GetCentazioPath("..", "test-generator");
  private readonly string SYSTEM_NAME = "Acme";
  
  private CentazioCodeGenerator nugetgen;
  private CentazioCodeGenerator refgen; 
  
  // the generator installs `Centazio.Core` and the core storage provider, which brings its own Centazio dependency
  private readonly List<string> PACKED_PROJECTS = [
    Path.Combine("Centazio.Core"),
    Path.Combine("Centazio.Providers", "Centazio.Providers.EF"),
    Path.Combine("Centazio.Providers", "Centazio.Providers.Sqlite")
  ];
  
  // nuget-mode solutions are generated under a directory holding a `nuget.config` that serves `Centazio.*` from the
  //    local feed and uses its own packages folder, so locally packed versions never reach the machine's global cache.
  //    ref-mode solutions are generated elsewhere so the restore of the referenced `src` projects is not affected
  private string WorkDir(bool usenuget) => Path.Combine(testdir, usenuget ? "nugets" : "refs");
  
  [OneTimeSetUp] public async Task CreateLocalPackageFeed() {
    if (Directory.Exists(testdir)) Directory.Delete(testdir, true);
    var (nugetdir, feed) = (Directory.CreateDirectory(WorkDir(true)).FullName, Directory.CreateDirectory(Path.Combine(testdir, "feed")).FullName);
    await PACKED_PROJECTS.Select(proj => runner.DotNet($"pack {FsUtils.GetCentazioPath("src", proj)} -c Release -o {feed}")).Synchronous();
    await File.WriteAllTextAsync(Path.Combine(nugetdir, "nuget.config"), 
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <config>
            <add key="globalPackagesFolder" value="{Path.Combine(testdir, "packages")}" />
          </config>
          <packageSources>
            <clear />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
            <add key="centazio-local" value="{feed}" />
          </packageSources>
          <packageSourceMapping>
            <packageSource key="nuget.org">
              <package pattern="*" />
            </packageSource>
            <packageSource key="centazio-local">
              <package pattern="Centazio.*" />
            </packageSource>
          </packageSourceMapping>
        </configuration>
        """);
  }
  
  [OneTimeTearDown] public void DeleteTestDir() {
    if (Directory.Exists(testdir)) Directory.Delete(testdir, true);
  }
  

  [SetUp] public async Task SetUp() {
    var settings = await F.Settings();
    nugetgen = new(new CommandRunner(), new Templater(settings));
    refgen = new(new CommandRunner(), new Templater(settings), false);
    Environment.SetEnvironmentVariable("IS_CLI", "true");
    FsUtils.TestingCliRootDir = FsUtils.GetCentazioPath();
  }

  // only the generated solutions are removed, the feed and its packages folder are kept for the whole fixture
  [TearDown] public void TearDown() {
    FsUtils.TestingCliRootDir = string.Empty;
    Environment.CurrentDirectory = properroot;
    Environment.SetEnvironmentVariable("IS_CLI", null);
    new List<bool> { true, false }.Select(usenuget => Path.Combine(WorkDir(usenuget), sln)).Where(Directory.Exists).ForEach(dir => Directory.Delete(dir, true));
  }

  [Test] public async Task Test_generate_solution_with_nugets() => 
      await GenerateSlnTestImpl(true);
  
  [Test] public async Task Test_generate_solution_with_references() => 
      await GenerateSlnTestImpl(false);
  
  [Test] public async Task Test_generate_project_for_each_lifecycle_stage_using_nugets() => 
      await GenerateFuncInOwnDirImpl(true);

  [Test] public async Task Test_generate_project_for_each_lifecycle_stage_using_refs() => 
      await GenerateFuncInOwnDirImpl(false);
  
  [Test] public async Task Test_generate_project_in_existing_assembly_using_nugets() => 
      await GenerateProjInExistingAssemblyImpl(true);

  [Test] public async Task Test_generate_project_in_existing_assembly_using_refs() => 
      await GenerateProjInExistingAssemblyImpl(false);
  
  private async Task GenerateSlnTestImpl(bool usenuget) {
    Environment.CurrentDirectory = Directory.CreateDirectory(WorkDir(usenuget)).FullName;
    var cmd = new GenerateSlnCommand(GetGen(usenuget));
    var existsbefore = Directory.Exists(sln);
    await cmd.ExecuteImpl(new GenerateSlnCommand.Settings { SolutionName = sln, CoreStorageProvider = "Sqlite" });

    Assert.That(existsbefore, Is.False);
    Assert.That(Directory.Exists(sln), Is.True);

    Environment.CurrentDirectory = Path.Combine(WorkDir(usenuget), sln);
    Assert.That(File.Exists(slnfile), Is.True);
    await ValidateProjectExistsInSln($"{sln}.Shared");

    await runner.DotNet("build", Environment.CurrentDirectory);
  }
  
  private async Task GenerateFuncInOwnDirImpl(bool usenuget) {
    await GenerateSlnTestImpl(usenuget);
    Environment.CurrentDirectory = Path.Combine(WorkDir(usenuget), sln);
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
      await runner.DotNet("build", func); // build only the project

      await Directory.GetFiles(func, "*.cs")
          .Select(async file => {
            var contents = await File.ReadAllTextAsync(file);
            Assert.That(contents.Contains("Sample"), Is.False, file);
            Assert.That(contents.Contains("ClickUp"), Is.False, file);
            Assert.That(contents.Contains("AppSheet"), Is.False, file);
            if (!file.EndsWith("Assembly.cs")) Assert.That(contents.Contains(system), Is.True, file);
          })
          .Synchronous();

      await runner.DotNet("build", Environment.CurrentDirectory); // build solution
    }
  }
  
  private async Task GenerateProjInExistingAssemblyImpl(bool usenuget) {
    await GenerateSlnTestImpl(usenuget);
    
    Environment.CurrentDirectory = Path.Combine(WorkDir(usenuget), sln);

    var cmd = new GenerateFunctionCommand(GetGen(usenuget));
    var sett = new Settings { SystemName = SYSTEM_NAME, Read = true };
    await cmd.ExecuteImpl(sett);

    sett = new Settings { SystemName = SYSTEM_NAME, AssemblyName = $"{SYSTEM_NAME}ReadFunction", Write = true };
    await cmd.ExecuteImpl(sett);

    Assert.That(Directory.Exists($"{SYSTEM_NAME}WriteFunction"), Is.False);
    Assert.That(File.Exists(Path.Combine($"{SYSTEM_NAME}ReadFunction", $"{SYSTEM_NAME}WriteFunction.cs")), Is.True);

    await runner.DotNet("build", $"{SYSTEM_NAME}ReadFunction"); // build only the project
    await runner.DotNet("build", Environment.CurrentDirectory); // build the whole solution
  }

  private CentazioCodeGenerator GetGen(bool usenuget) => usenuget ? nugetgen : refgen;
  
  private async Task ValidateProjectExistsInSln(string projname) =>
      Assert.That(Regex.Match(await File.ReadAllTextAsync(slnfile),
              @$"Project\(\""\{{.*}}\""\) = \""{projname}\"", \""{projname}\\{projname}.csproj\"", \""\{{.*}}\""")
          .Success);

}