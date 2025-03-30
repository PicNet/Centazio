using System.Reflection;
using System.Text.Json.Serialization;
using Azure.ResourceManager.AppService.Models;
using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Cli.Infra;

public enum ECloudEnv { Azure = 1, Aws = 2 }

// todo: pass settings in the ctor to avoid passing in each Name function
public class AzureFunctionProjectMeta(Assembly assembly, string generatedfolder) :  AbstractFunctionProjectMeta(assembly, generatedfolder) {
  public override string CloudName => ECloudEnv.Azure.ToString();
  public override string ProjectName => $"{Assembly.GetName().Name}.{CloudName}";
  
  public string GetFunctionAppName(CentazioSettings settings) {
    return DashedProjectName;
  }
  
  public string GetAppServicePlanName(CentazioSettings settings) {
    return settings.AzureSettings.AppServicePlan ?? $"{DashedProjectName}-Plan";
  }
  
  public AppServiceSkuDescription GetAppServiceSku(CentazioSettings settings) {
    // todo: read from function settings if not defaults
    return  new AppServiceSkuDescription { Name = "Y1", Tier = "Dynamic" };
  }

  public string GetWebSiteName(CentazioSettings settings) {
    return DashedProjectName;
  }

}

public class AwsFunctionProjectMeta(Assembly assembly, string generatedfolder, string function) :  AbstractFunctionProjectMeta(assembly, generatedfolder) {
  
  public readonly string AwsFunctionName = function;
  
  public override string CloudName => ECloudEnv.Aws.ToString();
  public override string ProjectName => $"{Assembly.GetName().Name}.{AwsFunctionName}.{CloudName}";
  
  public string HandlerName => $"{ProjectName}::{ProjectName}::{AwsFunctionName}Handler";
  public string RoleName => $"{DashedProjectName}-{AwsFunctionName}-role".ToLower();

}

public abstract class AbstractFunctionProjectMeta(Assembly assembly, string generatedfolder) {
  
  [JsonIgnore] public Assembly Assembly => assembly;
  
  public abstract string ProjectName { get; }
  public abstract string CloudName { get; }
  
  public string SolutionDirPath => Path.Combine(FsUtils.GetSolutionFilePath(), generatedfolder, ProjectName);
  public string ProjectDirPath => SolutionDirPath;
  public string CsprojFile => $"{ProjectName}.csproj";
  public string CsprojPath => Path.Combine(ProjectDirPath, $"{ProjectName}.csproj");
  public string SlnFilePath => Path.Combine(SolutionDirPath, $"{ProjectName}.sln");
  public string PublishPath => Path.Combine(ProjectDirPath, "bin", "Release", "net9.0", "publish");
  public string DashedProjectName => ProjectName.Replace('.', '-');
}

// disable warnings as these properties are only used by the template engine, so not detected
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedMember.Global
public record CsProjModel(string ProjectName) {
  public Guid ProjSolutionGuid { get; } = Guid.NewGuid();
  public Guid ProjGuid { get; } = Guid.NewGuid();
  
  public List<KeyValuePair<string, string>> GlobalProperties { get; } = [];
  public List<string> Files { get; } = [];
  public List<AssemblyRef> AssemblyReferences { get; } = [];
  public List<NuGetRef> NuGetReferences { get; } = [];
}

public record AssemblyRef(string FullName, string Path);
public record NuGetRef(string Name, string Version);