using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Gen;

public class CloudSolutionGeneratorTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  private readonly ICommandRunner cmd = new CommandRunner();
  
  [Test] public async Task Test_Azure_GenerateSolution() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    
    await CloudSolutionGenerator.Create(settings, templater, project, CentazioConstants.DEFAULT_ENVIRONMENT).GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionDirPath));

    var results = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.BuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
  
  [Test, Ignore("not implemented")] public async Task Test_Aws_GenerateSolution() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Aws);
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    
    await CloudSolutionGenerator.Create(settings, templater, project, CentazioConstants.DEFAULT_ENVIRONMENT).GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionDirPath));

    var results = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.BuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
  
  [Test, Ignore("This test is quite slow and we have already verified that it works")] public async Task Test_that_generating_solution_twice_works() {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    var generator = CloudSolutionGenerator.Create(settings, templater, project, CentazioConstants.DEFAULT_ENVIRONMENT);
    
    await generator.GenerateSolution();
    await generator.GenerateSolution();
    var results2 = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.BuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results2.Err));
  }
}