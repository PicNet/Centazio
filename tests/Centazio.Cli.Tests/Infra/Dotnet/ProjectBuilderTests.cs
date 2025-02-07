using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectBuilderTests {

  [Test] public async Task Test_BuildProject() {
    var settings = TestingFactories.Settings();
    var projpath = FsUtils.GetSolutionFilePath(settings.GeneratedCodeFolder, "Centazio.Cli.Tests.Azure");
    ProjectBuilder.Init();
    var results = await ProjectBuilder.BuildProject(projpath);
    Console.WriteLine(results);
  }

}