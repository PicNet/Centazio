using System.Reflection;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Build.Locator;

using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;

// todo: move to the Cli project
namespace Centazio.Cli.Infra.CodeGen.Csproj;

public enum ECloudEnv { Azure = 1, Aws = 2 }


// todo: this generator should use the latest version of neccessery nuget packages. Or we should have a 
//    test that hardcoded versions here are always the latest
// todo: how do we handle multiple functions in one project?
// todo: generated .Net package results in warning 'Found conflicts between different versions of "System.ClientModel" that could not be resolved.'
public class ProjectGenerator(string path, ECloudEnv cloud, Assembly assembly) {
  
  private readonly string NewAssemblyName = $"{assembly.GetName().Name}.{cloud}";
  
  public async Task GenerateSolution() {
    if (cloud != ECloudEnv.Azure) throw new NotImplementedException($"could environment[{cloud}] is not supported");
    
    var fullpath = GetSlnPath();
    var slnpath = await GenerateSolutionSkeleton(fullpath);
    EnhanceProjects(slnpath);
  }

  private string GetSlnPath() {
    var slnpath = Path.IsPathFullyQualified(path) ? path : Path.Combine(FsUtils.GetSolutionFilePath(), path, NewAssemblyName);
    Directory.CreateDirectory(slnpath);
    return slnpath;
  }

  private async Task<string> GenerateSolutionSkeleton(string fullpath) {
    var slnconf = new ConfigSln("Debug", "Any CPU");
    var project = new ProjectItem(ProjectType.CsSdk, $"{NewAssemblyName}\\{NewAssemblyName}.csproj", slnDir: fullpath);
    if (File.Exists(project.fullPath)) File.Delete(project.fullPath);
    var projconf = new ConfigPrj("Debug", "Any CPU", project.pGuid, build: true, slnconf);
    
    var hdata = new LhDataHelper();
    hdata.SetHeader(SlnHeader.MakeDefault())
        .SetProjects([project])
        .SetProjectConfigs([projconf])
        .SetSolutionConfigs([slnconf]);
    var slnpath = Path.Combine(fullpath, NewAssemblyName + ".sln");
    using var w = new SlnWriter(slnpath, hdata);
    w.Options |= SlnWriterOptions.CreateProjectsIfNotExist;
    await w.WriteAsync();
    return slnpath;
  }

  private void EnhanceProjects(string slnpath) {
    MSBuildLocator.RegisterDefaults();
    using Sln sln = new(slnpath, SlnItems.Env | SlnItems.LoadMinimalDefaultData);
    sln.Result.Env.Projects.ForEach(proj => {
      proj.SetProperties(new Dictionary<string, string> {
        { "TargetFramework", "net9.0" },
        { "LangVersion", "preview" },
        { "ImplicitUsings", "enable" },
        { "Nullable", "enable" },
        { "TreatWarningsAsErrors", "true" },
        { "EnforceCodeStyleInBuild", "true" }
      });
      if (cloud == ECloudEnv.Azure) { 
        AddAzureReferencesToProject(proj);
        AddAzureFunctionsToProject(proj);
      }
      else throw new NotImplementedException(cloud.ToString());
      
      proj.Save();
    });
  }

  private void AddAzureReferencesToProject(IXProject proj) {
    proj.AddPackageReference("Microsoft.Azure.Functions.Worker", "2.0.0");
    proj.AddPackageReference("Microsoft.Azure.Functions.Worker.Extensions.Timer", "4.3.1");
    proj.AddPackageReference("Microsoft.Azure.Functions.Worker.Sdk", "2.0.0");
    
    /*
     todo: add aws support:
      <PackageReference Include="Amazon.Lambda.Core" Version="2.1.0" />
      <PackageReference Include="Amazon.Lambda.APIGatewayEvents" Version="2.6.0" />
      <PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.3.1" />
     */
  }

  private void AddAzureFunctionsToProject(IXProject proj) {
    proj.AddReference(assembly);
    proj.AddReference(typeof(AbstractFunction<>).Assembly);
    
    IntegrationsAssemblyInspector.GetCentazioFunctions(assembly, []).ForEach(func => {
      var clcontent = @"
using Microsoft.Azure.Functions.Worker;
using Centazio.Core.Runner;
using {{FunctionNamespace}};

namespace {{NewAssemblyName}};

public class {{ClassName}}Azure {
  // static to avoid slow re-initialisation on warm start ups
  private static readonly Lazy<Task<IRunnableFunction>> impl = new(async () => await new FunctionInitialiser<{{ClassName}}>().Init(), LazyThreadSafetyMode.ExecutionAndPublication);

  [Function(""{{ClassName}}"")] public async Task Run([TimerTrigger(""* * * * * *"")] TimerInfo _) {
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