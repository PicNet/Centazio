using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectPublisherTests {
  
  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  
  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectPublisher(settings, templater).PublishProject);
  }
  
  private async Task Impl(Func<AzureFunctionProjectMeta, Task> builder) {
    var project = MiscHelpers.AzureEmptyFunctionProject();
    await new AzureCloudSolutionGenerator(settings, templater, project, CentazioConstants.DEFAULT_ENVIRONMENT).GenerateSolution();
    
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    await builder(project);
    Assert.That(Directory.Exists(project.PublishPath));
  }
}