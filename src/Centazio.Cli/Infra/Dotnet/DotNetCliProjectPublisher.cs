using Centazio.Core.Settings;

namespace Centazio.Cli.Infra.Dotnet;

public class DotNetCliProjectPublisher(CentazioSettings settings) : IProjectPublisher {

  private readonly CommandRunner cmd = new();
  
  public Task PublishProject(FunctionProjectMeta project) {
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    cmd.DotNet(settings.Parse(settings.Defaults.DotNetCleanProject), project.ProjectDirPath);
    cmd.DotNet(settings.Parse(settings.Defaults.DotNetPublishProject), project.ProjectDirPath);
    if (!Directory.Exists(project.PublishPath)) throw new Exception("publish failed");
    return Task.CompletedTask;
  }

}