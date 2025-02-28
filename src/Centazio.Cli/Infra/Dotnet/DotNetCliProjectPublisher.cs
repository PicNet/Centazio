using Centazio.Core.Settings;

namespace Centazio.Cli.Infra.Dotnet;

public class DotNetCliProjectPublisher(CentazioSettings settings, ITemplater templater) : IProjectPublisher {

  private readonly ICommandRunner cmd = new CommandRunner();
  
  public Task PublishProject(FunctionProjectMeta project) {
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    
    var results1 = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.CleanProject), project.ProjectDirPath, quiet: true);
    if (!results1.Success) { throw new Exception(results1.Err); }
    
    var results2 = cmd.DotNet(templater.ParseFromContent(settings.Defaults.ConsoleCommands.DotNet.PublishProject), project.ProjectDirPath, quiet: true);
    if (!results2.Success) { throw new Exception(results2.Err); }
    
    if (!Directory.Exists(project.PublishPath)) throw new Exception("publish failed");
    return Task.CompletedTask;
  }

}