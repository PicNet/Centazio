using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectPublisherTests {
  
  private CentazioSettings settings;
  private ICliSecretsManager secrets;
  private ITemplater templater;
  
  [SetUp] public async Task SetUp() {
    settings = await F.Settings();
    secrets = TestingCliSecretsManager.Instance;
    templater = new Templater(settings);
  }

  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectPublisher(settings, templater).PublishProject);
  }
  
  private async Task Impl(Func<AzFunctionProjectMeta, Task> builder) {
    var project = await MiscHelpers.AzEmptyFunctionProject();
    await new AzCloudSolutionGenerator(settings, secrets, templater, project, [CentazioConstants.DEFAULT_ENVIRONMENT]).GenerateSolution();
    
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    await builder(project);
    Assert.That(Directory.Exists(project.PublishPath));
  }
}