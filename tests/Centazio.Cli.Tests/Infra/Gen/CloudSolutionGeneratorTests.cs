using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra.Dotnet;

namespace Centazio.Cli.Tests.Infra.Gen;

public class CloudSolutionGeneratorTests {

  [Test] public async Task Test_GenerateSolution() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    
    await CloudSolutionGenerator.Create(project, "dev").GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionDirPath));

    var results = new CommandRunner().DotNet("build --configuration Release /property:GenerateFullPaths=true", project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
  
  [Test] public async Task Test_that_generating_solution_twice_works() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    
    await CloudSolutionGenerator.Create(project, "dev").GenerateSolution();
    await CloudSolutionGenerator.Create(project, "dev").GenerateSolution();
    
    var results = new CommandRunner().DotNet("build --configuration Release /property:GenerateFullPaths=true", project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
}