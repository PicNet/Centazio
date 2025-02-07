namespace Centazio.Cli.Infra.Dotnet;

public class DotNetCliProjectBuilder : IProjectBuilder {

  public Task<string> BuildProject(string projpath) {
    var project = projpath.Split('/', '\\').Last();
    var csproj = Path.Combine(projpath, project, project + ".csproj");
    new CommandRunner().DotNet("restore", projpath);
    new CommandRunner().DotNet("publish " + csproj);
    return Task.FromResult(Path.Combine(projpath, project, "bin", "Release", "net9.0", "publish"));
  }

}