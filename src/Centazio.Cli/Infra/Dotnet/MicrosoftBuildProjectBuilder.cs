using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace Centazio.Cli.Infra.Dotnet;

// current version of Microsft.Build: 17.12.6, SDK: 9.0.102 results in error: Could not load file or assembly 'NuGet.Frameworks, Version=6.12.2.1'
public class MicrosoftBuildProjectBuilder : IProjectBuilder {

  public MicrosoftBuildProjectBuilder() {
    var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
    MSBuildLocator.RegisterInstance(instances.OrderByDescending(instance => instance.Version).ElementAt(0));
  }
  
  public async Task<string> BuildProject(string projpath) {
    var project = projpath.Split('/', '\\').Last();
    var publishpath = Path.Combine(projpath, "bin", "Release", "net9.0", "publish");
    
    var logger = new ConsoleLogger(LoggerVerbosity.Normal);

    var collection = new ProjectCollection();
    var props = new Dictionary<string, string> {
      { "Configuration", "Release" },
      { "Platform", "AnyCPU" },
      { "PublishDir", publishpath },
      { "DeployOnBuild", "true" },
      { "PublishReadyToRun", "true" },
      { "SelfContained", "false" }
    };
    
    var csproj = Path.Combine(projpath, project, project + ".csproj");
    var proj = collection.LoadProject(csproj);
    var data = new BuildRequestData(proj.CreateProjectInstance(), ["Restore", "Publish"]);

    var parameters = new BuildParameters(collection) { Loggers = [logger], DetailedSummary = true, GlobalProperties = props};
    var result = await Task.Run(() => BuildManager.DefaultBuildManager.Build(parameters, data));
    if (result.OverallResult != BuildResultCode.Success) throw new Exception("Project publish failed");

    return publishpath;
  }
}