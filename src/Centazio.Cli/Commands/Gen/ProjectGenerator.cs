using System.Reflection;
using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Build.Locator;
using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

public enum ECloudEnv { Azure = 1, Aws = 2 }

public class ProjectGenerator(string path, ECloudEnv cloud, Assembly assembly) {
  
  private readonly string NewAssemblyName = $"{assembly.GetName().Name}.{cloud}";
  
  public async Task GenerateSolution() {
    if (cloud != ECloudEnv.Azure) throw new NotImplementedException($"could environment[{cloud}] is not supported");
    
    var fullpath = GetSlnPath();
    var slnpath = await GenerateSolutionSkeleton(fullpath);
    await EnhanceProjects(slnpath);
  }

  private string GetSlnPath() {
    var slnpath = Path.IsPathFullyQualified(path) ? path : Path.Combine(FsUtils.GetSolutionFilePath(), path, NewAssemblyName);
    Directory.CreateDirectory(slnpath);
    return slnpath;
  }

  private async Task<string> GenerateSolutionSkeleton(string fullpath) {
    var slnconfs = new ConfigSln[] { new("Debug", "Any CPU"), new("Release", "Any CPU") };
    var project = new ProjectItem(ProjectType.CsSdk, $"{NewAssemblyName}\\{NewAssemblyName}.csproj", slnDir: fullpath);
    if (File.Exists(project.fullPath)) File.Delete(project.fullPath);
    var projconfs = new ConfigPrj[] { new("Debug", "Any CPU", project.pGuid, build: true, slnconfs[0]), new("Debug", "Any CPU", project.pGuid, build: true, slnconfs[1]) };
    
    var hdata = new LhDataHelper();
    hdata.SetHeader(SlnHeader.MakeDefault())
        .SetProjects([project])
        .SetProjectConfigs(projconfs)
        .SetSolutionConfigs(slnconfs);
    var slnpath = Path.Combine(fullpath, NewAssemblyName + ".sln");
    using var w = new SlnWriter(slnpath, hdata);
    w.Options |= SlnWriterOptions.CreateProjectsIfNotExist;
    await w.WriteAsync();
    return slnpath;
  }

  private async Task EnhanceProjects(string slnpath) {
    MSBuildLocator.RegisterDefaults();
    using Sln sln = new(slnpath, SlnItems.Env | SlnItems.LoadMinimalDefaultData);
    foreach (var proj in sln.Result.Env.Projects) {
      proj.SetProperties(new Dictionary<string, string> {
        { "TargetFramework", "net9.0" },
        { "LangVersion", "preview" },
        { "ImplicitUsings", "enable" },
        { "Nullable", "enable" },
        { "TreatWarningsAsErrors", "true" },
        { "EnforceCodeStyleInBuild", "true" },
        { "ManagePackageVersionsCentrally", "false" }
      });
      if (cloud == ECloudEnv.Azure) { 
        await AddAzureReferencesToProject(proj);
        AddAzureFunctionsToProject(proj);
      }
      else throw new NotSupportedException(cloud.ToString());
      
      proj.Save();
    }
  }

  private Task AddAzureReferencesToProject(IXProject proj) {
    return AddLatestReferencesToProject(proj,
    [
      "Microsoft.Azure.Functions.Worker",
      "Microsoft.Azure.Functions.Worker.Sdk",
      "Microsoft.Azure.Functions.Worker.Extensions.Timer",
      "System.ClientModel", // needed to avoid `Found conflicts between different versions of "System.ClientModel" that could not be resolved`
      "Serilog"
    ]);
  }

  // todo: add aws Lambda support
  // private Task AddAwsReferencesToProject(IXProject proj) => AddLatestReferencesToProject(proj, ["Amazon.Lambda.Core", "Amazon.Lambda.APIGatewayEvents", "Amazon.Lambda.Serialization.SystemTextJson"]);

  private async Task AddLatestReferencesToProject(IXProject proj, List<string> packages) => 
      (await NugetHelpers.GetLatestStableVersions(packages)).ForEach(
          p => proj.AddPackageReference(p.name, p.version));

  private void AddAzureFunctionsToProject(IXProject proj) {
    proj.AddReference(assembly);
    proj.AddReference(typeof(AbstractFunction<>).Assembly);
    
    IntegrationsAssemblyInspector.GetCentazioFunctions(assembly, []).ForEach(func => {
      var clcontent = @"
using Centazio.Core.Runner;
using Centazio.Core.Misc;
using {{FunctionNamespace}};
using Microsoft.Azure.Functions.Worker;
using Serilog;

namespace {{NewAssemblyName}};

public class {{ClassName}}Azure {
  // static to avoid slow re-initialisation on warm start ups
  private static readonly Lazy<Task<IRunnableFunction>> impl = new(async () => await new FunctionsInitialiser().Init<{{ClassName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);

  [Function(""{{ClassName}}"")] public async Task Run([TimerTrigger(""* * * * * *"")] TimerInfo _) {
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    await (await impl.Value).RunFunction(); 
  }
}"
          .Replace("{{ClassName}}", func.Name)
          .Replace("{{FunctionNamespace}}", func.Namespace)
          .Replace("{{NewAssemblyName}}", NewAssemblyName);
      
      File.WriteAllText(Path.Combine(proj.ProjectPath, $"{func.Name}.cs"), clcontent);
    });
  }
}