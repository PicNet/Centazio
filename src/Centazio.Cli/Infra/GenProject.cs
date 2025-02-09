using System.Reflection;
using Centazio.Cli.Commands.Gen;
using Centazio.Core.Misc;

namespace Centazio.Cli.Infra;

public class GenProject(Assembly assembly, ECloudEnv cloud, string projfolder) {

  public Assembly Assembly => assembly;
  public ECloudEnv Cloud => cloud;
  
  public string ProjectName => $"{assembly.GetName().Name}.{cloud}";
  public string CsprojFile => $"{ProjectName}{Path.DirectorySeparatorChar}{ProjectName}.csproj";
  public string SolutionFolderPath => Path.Combine(FsUtils.GetSolutionFilePath(), projfolder, ProjectName);
  public string SlnFilePath => Path.Combine(SolutionFolderPath, $"{ProjectName}.sln");
}