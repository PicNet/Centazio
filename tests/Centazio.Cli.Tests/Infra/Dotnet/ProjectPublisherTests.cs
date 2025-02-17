using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectPublisherTests {
  
  private readonly CentazioSettings settings = TestingFactories.Settings();
  
  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectPublisher(settings).PublishProject);
  }
  
  private async Task Impl(Func<FunctionProjectMeta, Task> builder) {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    await CloudSolutionGenerator.Create(settings, project, CentazioConstants.DEFAULT_ENVIRONMENT).GenerateSolution();
    
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    await builder(project);
    Assert.That(Directory.Exists(project.PublishPath));
  }
}