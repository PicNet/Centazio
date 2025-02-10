﻿using Centazio.Cli.Infra;
using Centazio.Core.Runner;
using Microsoft.Build.Locator;
using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

public enum ECloudEnv { Azure = 1, Aws = 2 }

// todo: separate out into a hierarchy for Azure and Aws
public class ProjectGenerator(FunctionProjectMeta project) {
  
  public async Task GenerateSolution() {
    await GenerateSolutionSkeleton();
    await AddProjectsToSolution(project.SlnFilePath);
  }
  

  private async Task GenerateSolutionSkeleton() {
    Directory.CreateDirectory(project.SolutionPath);
    var (arch, configs) = ("Any CPU", new[] { "Debug", "Release" });
    var slnconfs = configs.Select(c => new ConfigSln(c, arch)).ToArray();
    var projitem = new ProjectItem(ProjectType.CsSdk, project.CsprojFile, slnDir: project.SolutionPath);
    if (File.Exists(projitem.fullPath)) File.Delete(projitem.fullPath);
    var projconfs = configs.Select((c, idx) => new ConfigPrj(c, arch, projitem.pGuid, build: true, slnconfs[idx])).ToArray();
    
    var hdata = new LhDataHelper();
    hdata.SetHeader(SlnHeader.MakeDefault())
        .SetProjects([projitem])
        .SetProjectConfigs(projconfs)
        .SetSolutionConfigs(slnconfs);
    using var w = new SlnWriter(project.SlnFilePath, hdata);
    w.Options |= SlnWriterOptions.CreateProjectsIfNotExist;
    await w.WriteAsync();
  }

  private async Task AddProjectsToSolution(string slnpath) {
    MSBuildLocator.RegisterDefaults();
    using Sln sln = new(slnpath, SlnItems.Env | SlnItems.LoadMinimalDefaultData);
    foreach (var proj in sln.Result.Env.Projects) {
      proj.SetProperties(new Dictionary<string, string> {
        { "TargetFramework", "net9.0" },
        { "AzureFunctionsVersion", "v4" },
        { "OutputType", "Exe" },
        { "ImplicitUsings", "enable" },
        { "Nullable", "enable" },
        { "TreatWarningsAsErrors", "true" },
        { "EnforceCodeStyleInBuild", "true" },
        { "ManagePackageVersionsCentrally", "false" }
      });
      if (project.Cloud == ECloudEnv.Azure) { 
        await AddAzureReferencesToProject(proj);
        await AddAzHostJsonFileToProject(proj);
        await AddAzureFunctionsToProject(proj);
      }
      else throw new NotSupportedException(project.Cloud.ToString());
      
      proj.Save();
    }
  }

  private Task AddAzureReferencesToProject(IXProject proj) {
    return AddLatestReferencesToProject(proj,
    [
      "Microsoft.Azure.Functions.Worker",
      "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore", // todo: required for `FunctionsApplicationBuilder.ConfigureFunctionsWebApplication`, can we remove all of this?
      "Microsoft.Azure.Functions.Worker.Extensions.Timer",
      "Microsoft.Azure.Functions.Worker.Sdk",
      "System.ClientModel", // needed to avoid `Found conflicts between different versions of "System.ClientModel" that could not be resolved`
      "Serilog"
    ]);
    
  }

  // todo: add aws Lambda support
  // private Task AddAwsReferencesToProject(IXProject proj) => AddLatestReferencesToProject(proj, ["Amazon.Lambda.Core", "Amazon.Lambda.APIGatewayEvents", "Amazon.Lambda.Serialization.SystemTextJson"]);

  private async Task AddLatestReferencesToProject(IXProject proj, List<string> packages) => 
      (await NugetHelpers.GetLatestStableVersions(packages)).ForEach(
          p => proj.AddPackageReference(p.name, p.version));

  // todo: read from external template file
  private async Task AddAzHostJsonFileToProject(IXProject proj) {
    proj.AddItem("None", "host.json", [new("CopyToOutputDirectory", "PreserveNewest")]);
    var contents = @"{
    ""version"": ""2.0"",
    ""logging"": {
        ""applicationInsights"": {
            ""samplingSettings"": {
                ""isEnabled"": true,
                ""excludedTypes"": ""Request""
            },
            ""enableLiveMetricsFilters"": true
        }
    }
}";
    await File.WriteAllTextAsync(Path.Combine(proj.ProjectPath, $"host.json"), contents);
  }
  
  private async Task AddAzureFunctionsToProject(IXProject proj) {
    var opts = AddReferenceOptions.Default | AddReferenceOptions.HidePrivate;
    proj.AddReference(project.Assembly, opts);
    proj.AddReference(typeof(AbstractFunction<>).Assembly, opts);

    // todo: these templates should be in other files to allow users to change the template if required
    foreach (var func in IntegrationsAssemblyInspector.GetCentazioFunctions(project.Assembly, [])) {
      var clcontent = @"
using Centazio.Core.Runner;
using Centazio.Core.Misc;
using {{FunctionNamespace}};
using Microsoft.Azure.Functions.Worker;
using Serilog;

namespace {{NewAssemblyName}};

public class {{ClassName}}Azure {  
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{ClassName}}Azure() {
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    impl = new(async () => await new FunctionsInitialiser().Init<{{ClassName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(""{{ClassName}}"")] public async Task Run([TimerTrigger(""* * * * * *"")] TimerInfo _) {    
    await (await impl.Value).RunFunction(); 
  }
}"
          .Replace("{{ClassName}}", func.Name)
          .Replace("{{FunctionNamespace}}", func.Namespace)
          .Replace("{{NewAssemblyName}}", proj.ProjectName);
      
      await File.WriteAllTextAsync(Path.Combine(proj.ProjectPath, $"{func.Name}.cs"), clcontent);
      await File.WriteAllTextAsync(Path.Combine(proj.ProjectPath, $"Program.cs"), @"using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Build().Run();");
    }
  }
}

