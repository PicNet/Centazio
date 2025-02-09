using Centazio.Cli.Infra;
using Centazio.Core.Runner;
using Microsoft.Build.Locator;
using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

public enum ECloudEnv { Azure = 1, Aws = 2 }

public class ProjectGenerator(GenProject meta) {
  
  public async Task GenerateSolution() {
    await GenerateSolutionSkeleton();
    await EnhanceProjects(meta.SlnFilePath);
  }
  

  private async Task GenerateSolutionSkeleton() {
    Directory.CreateDirectory(meta.SolutionFolderPath);
    var slnconfs = new ConfigSln[] { new("Debug", "Any CPU"), new("Release", "Any CPU") };
    var project = new ProjectItem(ProjectType.CsSdk, meta.CsprojFile, slnDir: meta.SolutionFolderPath);
    if (File.Exists(project.fullPath)) File.Delete(project.fullPath);
    var projconfs = new ConfigPrj[] { new("Debug", "Any CPU", project.pGuid, build: true, slnconfs[0]), new("Debug", "Any CPU", project.pGuid, build: true, slnconfs[1]) };
    
    var hdata = new LhDataHelper();
    hdata.SetHeader(SlnHeader.MakeDefault())
        .SetProjects([project])
        .SetProjectConfigs(projconfs)
        .SetSolutionConfigs(slnconfs);
    using var w = new SlnWriter(meta.SlnFilePath, hdata);
    w.Options |= SlnWriterOptions.CreateProjectsIfNotExist;
    await w.WriteAsync();
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
      if (meta.Cloud == ECloudEnv.Azure) { 
        await AddAzureReferencesToProject(proj);
        AddAzureFunctionsToProject(proj);
      }
      else throw new NotSupportedException(meta.Cloud.ToString());
      
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
    proj.AddReference(meta.Assembly);
    proj.AddReference(typeof(AbstractFunction<>).Assembly);
    
    IntegrationsAssemblyInspector.GetCentazioFunctions(meta.Assembly, []).ForEach(func => {
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
          .Replace("{{NewAssemblyName}}", proj.ProjectName);
      
      File.WriteAllText(Path.Combine(proj.ProjectPath, $"{func.Name}.cs"), clcontent);
    });
  }
}