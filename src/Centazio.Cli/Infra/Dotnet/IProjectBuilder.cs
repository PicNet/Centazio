namespace Centazio.Cli.Infra.Dotnet;

public interface IProjectBuilder {
  Task<string> BuildProject(string projpath);
}