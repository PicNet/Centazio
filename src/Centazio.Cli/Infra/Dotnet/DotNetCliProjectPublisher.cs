namespace Centazio.Cli.Infra.Dotnet;

// todo: all donet/az cli commands should be in a configuration file to allow users to change their choice of commands
public class DotNetCliProjectPublisher : IProjectPublisher {

  public Task BuildProject(FunctionProjectMeta project) {
    if (Directory.Exists(project.PublishPath)) Directory.Delete(project.PublishPath, true);
    new CommandRunner().DotNet("clean --configuration Release /property:GenerateFullPaths=true", project.ProjectPath);
    // new CommandRunner().DotNet("restore", project.ProjectPath);
    new CommandRunner().DotNet("publish --configuration Release /property:GenerateFullPaths=true", project.ProjectPath);
    if (!Directory.Exists(project.PublishPath)) throw new Exception("publish failed");
    return Task.CompletedTask;
  }

}