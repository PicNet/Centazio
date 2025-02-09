namespace Centazio.Cli.Infra.Dotnet;

public interface IProjectPublisher {
  Task BuildProject(GenProject project);
}