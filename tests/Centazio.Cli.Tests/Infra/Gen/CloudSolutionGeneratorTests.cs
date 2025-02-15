using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Gen;

public class CloudSolutionGeneratorTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly CommandRunner cmd = new();
  
  [Test] public async Task Test_GenerateSolution() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    
    await CloudSolutionGenerator.Create(settings, project, "dev").GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionDirPath));

    var results = cmd.DotNet(settings.Parse(settings.Defaults.DotNetBuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
  
  [Test] public async Task Test_that_generating_solution_twice_works() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    
    await CloudSolutionGenerator.Create(settings, project, "dev").GenerateSolution();
    await CloudSolutionGenerator.Create(settings, project, "dev").GenerateSolution();
    
    var results = cmd.DotNet(settings.Parse(settings.Defaults.DotNetBuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
}