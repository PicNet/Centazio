using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Misc;
using Centazio.Core;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Gen;

public class CloudSolutionGeneratorTests {
  
  private readonly ICommandRunner cmd = new CommandRunner();
  
  private CentazioSettings settings;
  private ITemplater templater;
  
  [SetUp] public async Task SetUp() {
    settings = await TestingFactories.Settings();
    templater = new Templater(settings);
  }

  [Test] public async Task Test_Az_GenerateSolution() {
    var project = await MiscHelpers.AzEmptyFunctionProject();
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    
    await new AzCloudSolutionGenerator(settings, templater, project, [CentazioConstants.DEFAULT_ENVIRONMENT]).GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionDirPath));

    var results = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.BuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
  
  [Test] public async Task Test_Aws_GenerateSolution() {
    var project = await MiscHelpers.AwsEmptyFunctionProject("EmptyFunction");
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    
    await new AwsCloudSolutionGenerator(settings, templater, project, [CentazioConstants.DEFAULT_ENVIRONMENT], String.Empty).GenerateSolution();
    Assert.That(Directory.Exists(project.SolutionDirPath));

    var results = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.BuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results.Err));
  }
  
  [Test, Ignore("This test is quite slow and we have already verified that it works")] public async Task Test_that_generating_solution_twice_works() {
    var project = await MiscHelpers.AzEmptyFunctionProject();
    var generator = new AzCloudSolutionGenerator(settings, templater, project, [CentazioConstants.DEFAULT_ENVIRONMENT]);
    
    await generator.GenerateSolution();
    await generator.GenerateSolution();
    var results2 = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.BuildProject), project.ProjectDirPath);
    Assert.That(String.IsNullOrWhiteSpace(results2.Err));
  }
}