using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace Centazio.Cli.Infra.Dotnet;

// current version of Microsft.Build: 17.12.6, SDK: 9.0.102 results in error: Could not load file or assembly 'NuGet.Frameworks, Version=6.12.2.1'
public class MicrosoftBuildProjectPublisher : IProjectPublisher {

  public MicrosoftBuildProjectPublisher() {
    var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
    MSBuildLocator.RegisterInstance(instances.OrderByDescending(instance => instance.Version).ElementAt(0));
  }
  
  public async Task BuildProject(GenProject project) {
    var logger = new ConsoleLogger(LoggerVerbosity.Normal);

    var collection = new ProjectCollection();
    var props = new Dictionary<string, string> {
      { "Configuration", "Release" },
      { "Platform", "AnyCPU" },
      { "PublishDir", project.PublishPath },
      { "DeployOnBuild", "true" },
      { "PublishReadyToRun", "true" },
      { "SelfContained", "false" }
    };
    
    var proj = collection.LoadProject(project.CsprojPath);
    var data = new BuildRequestData(proj.CreateProjectInstance(), ["Restore", "Publish"]);

    var parameters = new BuildParameters(collection) { Loggers = [logger], DetailedSummary = true, GlobalProperties = props};
    var result = await Task.Run(() => BuildManager.DefaultBuildManager.Build(parameters, data));
    if (result.OverallResult != BuildResultCode.Success) throw new Exception("publish failed");
  }
}