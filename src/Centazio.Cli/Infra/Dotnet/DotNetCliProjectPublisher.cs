namespace Centazio.Cli.Infra.Dotnet;

public class DotNetCliProjectPublisher : IProjectPublisher {

  public Task BuildProject(GenProject project) {
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    new CommandRunner().DotNet("restore", project.ProjectPath);
    new CommandRunner().DotNet("publish " + project.CsprojPath);
    if (!Directory.Exists(project.PublishPath)) throw new Exception("publish failed");
    return Task.CompletedTask;
  }

}