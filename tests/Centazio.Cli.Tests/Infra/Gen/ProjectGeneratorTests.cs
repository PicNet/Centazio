using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra.Dotnet;

namespace Centazio.Cli.Tests.Infra.Gen;

public class ProjectGeneratorTests {

  [Test] public async Task Test_GenerateSolution() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    if (Directory.Exists(project.SolutionPath)) Directory.Delete(project.SolutionPath, true);
    
    await new ProjectGenerator(project).GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionPath));

    var results = new CommandRunner().DotNet("build --configuration Release /property:GenerateFullPaths=true", project.ProjectPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }

}