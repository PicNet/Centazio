﻿using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Dotnet;

public class ProjectPublisherTests {
  
  [Test, Ignore("does not work with current sdk")] public async Task Test_MsBuildProjectBuilder_BuildProject() {
    await Impl(new MicrosoftBuildProjectPublisher().PublishProject);
  }

  [Test] public async Task Test_DotNetCliProjectBuilder_BuildProject() {
    await Impl(new DotNetCliProjectPublisher(TestingFactories.Settings()).PublishProject);
  }
  
  private async Task Impl(Func<FunctionProjectMeta, Task> builder) {
    var project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
    await CloudSolutionGenerator.Create(project, "dev").GenerateSolution();
    
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    await builder(project);
    Assert.That(Directory.Exists(project.PublishPath));
  }
}