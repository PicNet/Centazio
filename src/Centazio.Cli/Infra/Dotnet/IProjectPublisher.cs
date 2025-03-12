namespace Centazio.Cli.Infra.Dotnet;

public interface IProjectPublisher {
  Task PublishProject(AbstractFunctionProjectMeta project);
}