namespace Centazio.Cli.Infra.Dotnet;

public interface IProjectPublisher {
  Task BuildProject(FunctionProjectMeta project);
}