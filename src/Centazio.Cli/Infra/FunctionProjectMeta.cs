using System.Reflection;
using System.Text.Json.Serialization;
using Centazio.Cli.Commands.Gen;
using Centazio.Core.Misc;

namespace Centazio.Cli.Infra;

public class FunctionProjectMeta(Assembly assembly, ECloudEnv cloud, string generatedfolder) {
  
  // todo: fix single folder deployment, i.e. where sln and csproj in on directory
  private readonly bool SINGLE_FOLDER = false; 
  
  [JsonIgnore] public Assembly Assembly => assembly;
  public ECloudEnv Cloud => cloud;
  
  public string ProjectName => $"{assembly.GetName().Name}.{cloud}";
  public string SolutionDirPath => Path.Combine(FsUtils.GetSolutionFilePath(), generatedfolder, ProjectName);
  public string ProjectDirPath => SINGLE_FOLDER ? SolutionDirPath : Path.Combine(SolutionDirPath, ProjectName);
  public string CsprojFile => SINGLE_FOLDER ? $"{ProjectName}.csproj" : Path.Combine(ProjectName, $"{ProjectName}.csproj");
  public string CsprojPath => Path.Combine(ProjectDirPath, $"{ProjectName}.csproj");
  public string SlnFilePath => Path.Combine(SolutionDirPath, $"{ProjectName}.sln");
  public string PublishPath => Path.Combine(ProjectDirPath, "bin", "Release", "net9.0", "publish");
  public string DashedProjectName => ProjectName.Replace('.', '-');
}