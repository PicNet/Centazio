using System.Reflection;
using System.Text.Json.Serialization;
using Azure.ResourceManager.AppService.Models;
using Centazio.Core.Settings;

namespace Centazio.Cli.Infra;

public enum ECloudEnv { Azure = 1, Aws = 2 }

public class AzureFunctionProjectMeta(Assembly assembly, CentazioSettings settings, ITemplater templater) :  AbstractFunctionProjectMeta(assembly, settings) {
  
  public override string CloudName => ECloudEnv.Azure.ToString();
  public override string ProjectName => $"{Assembly.GetName().Name}.{CloudName}";
  
  private readonly AzureSettings azsett = settings.AzureSettings;
  private readonly AzFunctionsSettings? funcsett = settings.AzureSettings.AzFunctions.SingleOrDefault(f => f.Assembly == assembly.GetName().Name);
  
  public string GetFunctionAppName() => 
      funcsett?.FunctionAppName ?? 
      azsett.FunctionAppName ??
      templater.ParseFromContent(azsett.FunctionAppNameTemplate, this);

  public string GetAppServicePlanName() =>
      funcsett?.AppServicePlanName ?? 
      azsett.AppServicePlanName ??
      templater.ParseFromContent(azsett.AppServicePlanNameTemplate, this);

  public AppServiceSkuDescription GetAppServiceSku() => new() { 
    Name = funcsett?.AppServiceSkuName ?? azsett.AppServiceSkuName, 
    Tier = funcsett?.AppServiceSkuTier ?? azsett.AppServiceSkuTier 
  };

  public string GetWebSiteName() => 
      funcsett?.WebSiteName ?? 
      azsett.WebSiteName ??
      templater.ParseFromContent(azsett.WebSiteNameTemplate, this);

}

public class AwsFunctionProjectMeta(Assembly assembly, CentazioSettings settings, string function) :  AbstractFunctionProjectMeta(assembly, settings) {
  
  public readonly string AwsFunctionName = function;
  
  public override string CloudName => ECloudEnv.Aws.ToString();
  public override string ProjectName => $"{Assembly.GetName().Name}.{AwsFunctionName}.{CloudName}";
  
  public string HandlerName => $"{ProjectName}::{ProjectName}::{AwsFunctionName}Handler";
  public string RoleName => $"{DashedProjectName}-{AwsFunctionName}-role".ToLower();

}

public abstract class AbstractFunctionProjectMeta(Assembly assembly, CentazioSettings settings) {
  
  [JsonIgnore] protected readonly CentazioSettings settings = settings;
  [JsonIgnore] public Assembly Assembly => assembly;
  
  public abstract string ProjectName { get; }
  public abstract string CloudName { get; }
  
  // the root directory (solution dir) should always be relative to the cwd
  public string SolutionDirPath => Env.IsInDev() ? FsUtils.GetDevPath(settings.Defaults.GeneratedCodeFolder, ProjectName) : Path.Combine(Environment.CurrentDirectory, settings.Defaults.GeneratedCodeFolder, ProjectName);
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