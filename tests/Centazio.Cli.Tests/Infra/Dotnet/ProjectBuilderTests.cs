using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectBuilderTests {

  [Test, Ignore("does not work with current sdk")] public async Task Test_MsBuildProjectBuilder_BuildProject() {
    await Impl(new MicrosoftBuildProjectBuilder().BuildProject);
  }

  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectBuilder().BuildProject);
  }
  
  private async Task Impl(Func<string, Task<string>> builder) {
    var settings = TestingFactories.Settings();
    var project = $"{GetType().Assembly.GetName().Name}.{ECloudEnv.Azure}";
    var projpath = FsUtils.GetSolutionFilePath(settings.GeneratedCodeFolder, project);
    var exppath = Path.Combine(projpath, project, "bin", "Release", "net9.0", "publish");
    if (Directory.Exists(exppath)) Directory.Delete(exppath, true);
    
    var publishdir = await builder(projpath);
    
    Assert.That(publishdir, Is.EqualTo(exppath));
    Assert.That(Directory.Exists(publishdir));
  }
}