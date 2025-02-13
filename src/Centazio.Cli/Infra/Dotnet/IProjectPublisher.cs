namespace Centazio.Cli.Infra.Dotnet;

public interface IProjectPublisher {
  Task PublishProject(FunctionProjectMeta project);
}