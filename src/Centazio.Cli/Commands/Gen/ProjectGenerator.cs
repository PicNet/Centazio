using System.Reflection;
using Centazio.Cli.Infra;
using Centazio.Core;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Build.Locator;
using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;
using ReflectionUtils = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Cli.Commands.Gen;

public enum ECloudEnv { Azure = 1, Aws = 2 }

// todo: once IXProject is created, create a new class to avoid having `proj` in every method
public abstract class ProjectGenerator(FunctionProjectMeta project, string environment) {
  
  public static ProjectGenerator Create(FunctionProjectMeta project, string environment) {
    ValidateProjectAssemblyBeforeCodeGen(project.Assembly);
    switch (project.Cloud) {
      case ECloudEnv.Aws: return new AwsProjectGenerator(project, environment);
      case ECloudEnv.Azure: return new AzureProjectGenerator(project, environment);
      default: throw new NotSupportedException(project.Cloud.ToString());
      
    }
  }

  private static void ValidateProjectAssemblyBeforeCodeGen(Assembly ass) {
    IntegrationsAssemblyInspector.GetCentazioIntegration(ass); // throws own error
    if (!IntegrationsAssemblyInspector.GetCentazioFunctions(ass, []).Any()) throw new Exception($"no valid Centazio Functions found in assembly[{ass.GetName().FullName}]");
  }

  public async Task GenerateSolution() {
    await GenerateSolutionSkeleton();
    await AddProjectsToSolution(project.SlnFilePath);
  }
  
  private async Task GenerateSolutionSkeleton() {
    Directory.CreateDirectory(project.SolutionDirPath);
    var (arch, configs) = ("Any CPU", new[] { "Debug", "Release" });
    var slnconfs = configs.Select(c => new ConfigSln(c, arch)).ToArray();
    var projitem = new ProjectItem(ProjectType.CsSdk, project.CsprojFile, slnDir: project.SolutionDirPath);
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
        { "OutputType", "Exe" },
        { "ImplicitUsings", "enable" },
        { "Nullable", "enable" },
        { "TreatWarningsAsErrors", "true" },
        { "EnforceCodeStyleInBuild", "true" },
        { "ManagePackageVersionsCentrally", "false" }
      });
      AddSettingsFilesToProject(proj);
      var added = new Dictionary<string, bool>();
      AddCentazioProjectReferencesToProject(proj, added);
      await AddCentazioProvidersAndRelatedNugetsToProject(proj, added);
      await AddCentazioNuGetReferencesToProject(proj);
      
      var functions = IntegrationsAssemblyInspector.GetCentazioFunctions(project.Assembly, []);
      
      await AddCloudSpecificContentToProject(proj, functions);
      proj.Save();
    }
  }

  private void AddCentazioProjectReferencesToProject(IXProject proj, IDictionary<string, bool> added) {
    AddReferenceIfRequired(typeof(AbstractFunction<>).Assembly, proj, added); // Add Centazio.Core
    AddReferenceIfRequired(project.Assembly, proj, added); // Add this function's assemply
  }
  
  private async Task AddCentazioProvidersAndRelatedNugetsToProject(IXProject proj, IDictionary<string, bool> added) {
    var settings = new SettingsLoader().Load<CentazioSettings>(environment);
    var providers = new [] { settings.StagedEntityRepository.Provider, settings.CtlRepository.Provider }.Distinct().ToList();
    var provasses = providers.Select(prov => IntegrationsAssemblyInspector.GetCoreServiceFactoryType<IServiceFactory<IStagedEntityRepository>>(prov).Assembly).Distinct().ToList();
    
    provasses.ForEach(ass => AddReferenceIfRequired(ass, proj, added));
    
    var references = provasses.SelectMany(provass => 
        provass.GetReferencedAssemblies()
            .Select(a => a.Name ?? throw new Exception())
            .Where(name => !name.StartsWith("System") && !added.ContainsKey(name))).ToList();
    
    var centazios = references.Where(name => name.StartsWith($"{nameof(Centazio)}.")).ToList();
    var nugets = references.Where(name => !name.StartsWith($"{nameof(Centazio)}.")).ToList();

    centazios.ForEach(name => AddReferenceIfRequired(ReflectionUtils.LoadAssembly(name), proj, added));
    await AddLatestNuGetReferencesToProject(proj, nugets);
  }

  void AddReferenceIfRequired(Assembly ass, IXProject proj, IDictionary<string, bool> added) {
    var name = ass.GetName().Name ?? throw new Exception();
    if (!added.TryAdd(name, true)) return;

    proj.AddReference(ass, AddReferenceOptions.Default | AddReferenceOptions.HidePrivate);
  }
  
  private Task AddCentazioNuGetReferencesToProject(IXProject proj) {
    return AddLatestNuGetReferencesToProject(proj,
    [
      "Serilog",
      "Serilog.Sinks.Console",
      "System.ClientModel" // not required but avoids versioning warnings
    ]);
  }

  private void AddSettingsFilesToProject(IXProject proj) {
    var files = new SettingsLoader().GetSettingsFilePathList(environment);
    files.ForEach(path => {
      var fname = Path.GetFileName(path);
      proj.AddItem("None", fname, [new("CopyToOutputDirectory", "PreserveNewest")]);
      File.Copy(path, Path.Combine(proj.ProjectPath, fname), true);
    });
  }

  protected async Task AddLatestNuGetReferencesToProject(IXProject proj, List<string> packages) => 
      (await NugetHelpers.GetLatestStableVersions(packages)).ForEach(
          p => proj.AddPackageReference(p.name, p.version));
  
  protected abstract Task AddCloudSpecificContentToProject(IXProject proj, List<Type> functions);
}

internal class AzureProjectGenerator(FunctionProjectMeta project, string environment) : ProjectGenerator(project, environment) {

  protected override async Task AddCloudSpecificContentToProject(IXProject proj, List<Type> functions) {
    await AddAzureNuGetReferencesToProject(proj);
    await AddAzHostJsonFileToProject(proj);
    await AddAzureFunctionsToProject(proj, functions);
  }
  
  private Task AddAzureNuGetReferencesToProject(IXProject proj) => 
      AddLatestNuGetReferencesToProject(proj, [
        "Microsoft.Azure.Functions.Worker",
        "Microsoft.Azure.Functions.Worker.Extensions.Timer",
        "Microsoft.Azure.Functions.Worker.Sdk"
      ]);

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
  
  private async Task AddAzureFunctionsToProject(IXProject proj, List<Type> functions) {
    // todo: these templates should be in other files to allow users to change the template if required
    foreach (var func in functions) {
      var clcontent = @"
using Microsoft.Azure.Functions.Worker;
using Centazio.Core.Runner;

namespace {{NewAssemblyName}};

public class {{ClassName}}Azure {  
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{ClassName}}Azure() {    
    impl = new(async () => await new FunctionsInitialiser().Init<{{ClassName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(nameof({{ClassName}}))] public async Task Run([TimerTrigger(""*/10 * * * * *"")] TimerInfo timer) {    
    await (await impl.Value).RunFunction(); 
  }
}"
          .Replace("{{ClassName}}", func.Name)
          .Replace("{{FunctionNamespace}}", func.Namespace)
          .Replace("{{NewAssemblyName}}", proj.ProjectName);
      
      await File.WriteAllTextAsync(Path.Combine(proj.ProjectPath, $"{func.Name}.cs"), clcontent);
      await File.WriteAllTextAsync(Path.Combine(proj.ProjectPath, $"Program.cs"), @"
using Centazio.Core.Misc;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();

new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()  
  .Build().Run();
");
    }
  }
}

internal class AwsProjectGenerator(FunctionProjectMeta project, string environment) : ProjectGenerator(project, environment) {
  
  protected override Task AddCloudSpecificContentToProject(IXProject proj, List<Type> functions) => throw new NotImplementedException();

}