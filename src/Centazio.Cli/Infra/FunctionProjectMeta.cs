using System.Reflection;
using System.Text.Json.Serialization;
using Centazio.Cli.Commands.Gen;
using Centazio.Core.Misc;

namespace Centazio.Cli.Infra;

public class FunctionProjectMeta(Assembly assembly, ECloudEnv cloud, string generatedfolder) {
  
  [JsonIgnore] public Assembly Assembly => assembly;
  public ECloudEnv Cloud => cloud;

  public Guid ProjSolutionGuid { get; } = Guid.NewGuid();
  public Guid ProjGuid { get; } = Guid.NewGuid();
  public string ProjectName => $"{assembly.GetName().Name}.{cloud}";
  public string SolutionDirPath => Path.Combine(FsUtils.GetSolutionFilePath(), generatedfolder, ProjectName);
  public string ProjectDirPath => SolutionDirPath;
  public string CsprojFile => $"{ProjectName}.csproj";
  public string CsprojPath => Path.Combine(ProjectDirPath, $"{ProjectName}.csproj");
  public string SlnFilePath => Path.Combine(SolutionDirPath, $"{ProjectName}.sln");
  public string PublishPath => Path.Combine(ProjectDirPath, "bin", "Release", "net9.0", "publish");
  public string DashedProjectName => ProjectName.Replace('.', '-');
  
  public List<KeyValuePair<string, string>> GlobalProperties { get; } = new();
  public List<string> Files { get; } = new();
  public List<AssemblyRef> AssemblyReferences { get; } = new();
  public List<NuGetRef> NuGetReferences { get; } = new();
}

public record AssemblyRef(string FullName, string Path);
public record NuGetRef(string Name, string Version);