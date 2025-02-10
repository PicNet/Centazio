using System.Reflection;
using System.Text.Json.Serialization;
using Centazio.Cli.Commands.Gen;
using Centazio.Core.Misc;

namespace Centazio.Cli.Infra;

public class FunctionProjectMeta(Assembly assembly, ECloudEnv cloud, string generatedfolder) {
  
  [JsonIgnore] public Assembly Assembly => assembly;
  public ECloudEnv Cloud => cloud;
  
  public string ProjectName => $"{assembly.GetName().Name}.{cloud}";
  public string SolutionPath => Path.Combine(FsUtils.GetSolutionFilePath(), generatedfolder, ProjectName);
  public string ProjectPath => Path.Combine(SolutionPath, ProjectName);
  public string CsprojFile => Path.Combine(ProjectName, $"{ProjectName}.csproj");
  public string CsprojPath => Path.Combine(ProjectPath, $"{ProjectName}.csproj");
  public string SlnFilePath => Path.Combine(SolutionPath, $"{ProjectName}.sln");
  public string PublishPath => Path.Combine(ProjectPath, "bin", "Release", "net9.0", "publish");
  public string DashedProjectName => ProjectName.Replace('.', '-');
}