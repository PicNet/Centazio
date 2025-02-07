using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace Centazio.Cli.Infra.Dotnet;

public class ProjectBuilder {

  public static void Init() {
    var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
    MSBuildLocator.RegisterInstance(instances.OrderByDescending(instance => instance.Version).ElementAt(1));
  }
  
  public static async Task<string> BuildProject(string projpath) {
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
    
    var proj = collection.LoadProject(Path.Combine(projpath, project, project + ".csproj"));
    var data = new BuildRequestData(proj.CreateProjectInstance(), ["Restore", "Publish"]);

    var parameters = new BuildParameters(collection) { Loggers = [logger], DetailedSummary = true, GlobalProperties = props};
    var result = await Task.Run(() => BuildManager.DefaultBuildManager.Build(parameters, data));
    if (result.OverallResult != BuildResultCode.Success) throw new Exception("Project publish failed");

    return publishpath;
  }

}