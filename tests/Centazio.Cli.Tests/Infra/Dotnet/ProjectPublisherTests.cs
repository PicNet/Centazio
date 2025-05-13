using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectPublisherTests {
  
  private CentazioSettings settings;
  private ITemplater templater;
  
  [SetUp] public async Task SetUp() {
    settings = await TestingFactories.Settings();
    templater = new Templater(settings);
  }

  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectPublisher(settings, templater).PublishProject);
  }
  
  private async Task Impl(Func<AzFunctionProjectMeta, Task> builder) {
    var project = await MiscHelpers.AzEmptyFunctionProject();
    await new AzCloudSolutionGenerator(settings, templater, project, [CentazioConstants.DEFAULT_ENVIRONMENT]).GenerateSolution();
    
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    await builder(project);
    Assert.That(Directory.Exists(project.PublishPath));
  }
}