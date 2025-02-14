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

}