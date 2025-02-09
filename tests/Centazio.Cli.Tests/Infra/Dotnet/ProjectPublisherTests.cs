using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectPublisherTests {

  [Test, Ignore("does not work with current sdk")] public async Task Test_MsBuildProjectBuilder_BuildProject() {
    await Impl(new MicrosoftBuildProjectPublisher().BuildProject);
  }

  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectPublisher().BuildProject);
  }
  
  private async Task Impl(Func<GenProject, Task> builder) {
    var settings = TestingFactories.Settings();
    var project =  new GenProject(GetType().Assembly, ECloudEnv.Azure, settings.GeneratedCodeFolder);
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    
    await builder(project);
    
    Assert.That(Directory.Exists(project.PublishPath));
  }
}