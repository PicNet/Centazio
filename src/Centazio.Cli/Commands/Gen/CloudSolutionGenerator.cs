using System.Reflection;
using Centazio.Cli.Infra;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Build.Locator;
using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;
using ReflectionUtils = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Cli.Commands.Gen;

public enum ECloudEnv { Azure = 1, Aws = 2 }

public abstract class CloudSolutionGenerator(FunctionProjectMeta project, string environment) {
  
  protected readonly FunctionProjectMeta project = project;
  protected readonly string environment = environment;
  
  public static CloudSolutionGenerator Create(CentazioSettings settings, FunctionProjectMeta project, string environment) {
    ValidateProjectAssemblyBeforeCodeGen(project.Assembly);
    switch (project.Cloud) {
      case ECloudEnv.Aws: return new AwsCloudSolutionGenerator(settings, project, environment);
      case ECloudEnv.Azure: return new AzureCloudSolutionGenerator(settings, project, environment);
      default: throw new NotSupportedException(project.Cloud.ToString());
      
    }
    
    void ValidateProjectAssemblyBeforeCodeGen(Assembly ass) {
      IntegrationsAssemblyInspector.GetCentazioIntegration(ass, environment); // throws own error
      if (!IntegrationsAssemblyInspector.GetCentazioFunctions(ass, []).Any()) throw new Exception($"no valid Centazio Functions found in assembly[{ass.GetName().FullName}]");
    }
  }

  public async Task GenerateSolution() {
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    Directory.CreateDirectory(project.SolutionDirPath);
    
    await GenerateSolutionSkeleton();
    await AddProjectsToSolution();
  }
  
  private async Task GenerateSolutionSkeleton() {
    var (arch, configs) = ("Any CPU", new[] { "Debug", "Release" });
    var slnconfs = configs.Select(c => new ConfigSln(c, arch)).ToArray();
    var projitem = new ProjectItem(ProjectType.CsSdk, project.CsprojFile, slnDir: project.SolutionDirPath);
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

  private async Task AddProjectsToSolution() {
    if (MSBuildLocator.CanRegister)
      MSBuildLocator.RegisterInstance(
          MSBuildLocator.QueryVisualStudioInstances().OrderBy(i => i.Version).Last());
    
    using var sln = new Sln(project.SlnFilePath, SlnItems.Env | SlnItems.LoadMinimalDefaultData);
    await sln.Result.Env.Projects.ForEachSequentialAsync(async proj => {
      ClearProjectToAvoidDupsBug(proj);
      await GetCloudProjectGenerator(proj).Generate();
    });

    void ClearProjectToAvoidDupsBug(IXProject proj) {
      proj.GetPackageReferences().ToList().ForEach(r => proj.RemovePackageReference(r.evaluated));
      proj.GetProjectReferences().ToList().ForEach(r => proj.RemoveProjectReference(r.evaluated));
      proj.GetReferences().ToList().ForEach(r => proj.RemoveReference(r.evaluated));
      proj.GetImports().ToList().ForEach(i => proj.RemoveImport(i));
      // very slow in debugger
      if (!System.Diagnostics.Debugger.IsAttached) {
        proj.GetProperties().ToList().ForEach(p => proj.RemoveProperty(p));
        proj.GetItems().ToList().ForEach(i => { try { proj.RemoveItem(i); } catch (InvalidOperationException) {} });
      }
    }
  }

  protected abstract AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj);

}

public abstract class AbstractCloudProjectGenerator(CentazioSettings settings, FunctionProjectMeta projmeta, IXProject slnproj, string environment) {
  
  protected readonly CentazioSettings settings = settings;
  protected readonly IXProject slnproj = slnproj;
  protected readonly string environment = environment;

  public async Task Generate() {
    slnproj.SetProperties(new Dictionary<string, string> {
      { "TargetFramework", "net9.0" },
      { "OutputType", "Exe" },
      { "ImplicitUsings", "enable" },
      { "Nullable", "enable" },
      { "TreatWarningsAsErrors", "true" },
      { "EnforceCodeStyleInBuild", "true" },
      { "ManagePackageVersionsCentrally", "false" }
    });
    AddSettingsFilesToProject();
    AddSecretsFilesToProject();
    
    var added = new Dictionary<string, bool>();
    AddCentazioProjectReferencesToProject(added);
    await AddCentazioProvidersAndRelatedNugetsToProject(added);
    await AddCentazioNuGetReferencesToProject(added);
    
    var functions = IntegrationsAssemblyInspector.GetCentazioFunctions(projmeta.Assembly, []);
    await AddCloudSpecificContentToProject(functions, added);
    
    slnproj.Save();
  }

  private void AddCentazioProjectReferencesToProject(Dictionary<string, bool> added) {
    AddReferenceIfRequired(typeof(AbstractFunction<>).Assembly, added); // Add Centazio.Core
    AddReferenceIfRequired(projmeta.Assembly, added); // Add this function's assemply
  }
  
  private async Task AddCentazioProvidersAndRelatedNugetsToProject(Dictionary<string, bool> added) {
    var providers = new [] { settings.StagedEntityRepository.Provider, settings.CtlRepository.Provider }.Distinct().ToList();
    var provasses = providers.Select(prov => IntegrationsAssemblyInspector.GetCoreServiceFactoryType<IServiceFactory<IStagedEntityRepository>>(prov).Assembly).Distinct().ToList();
    
    provasses.ForEach(ass => AddReferenceIfRequired(ass, added));
    
    var references = provasses.SelectMany(provass => 
        provass.GetReferencedAssemblies()
            .Select(a => a.Name ?? throw new Exception())
            .Where(name => !name.StartsWith("System") && !added.ContainsKey(name))).ToList();
    
    var centazios = references.Where(name => name.StartsWith($"{nameof(Centazio)}.")).ToList();
    var nugets = references.Where(name => !name.StartsWith($"{nameof(Centazio)}.")).ToList();

    centazios.ForEach(name => AddReferenceIfRequired(ReflectionUtils.LoadAssembly(name), added));
    await AddLatestNuGetReferencesToProject(nugets, added);
  }

  void AddReferenceIfRequired(Assembly ass, Dictionary<string, bool> added) {
    var name = ass.GetName().Name ?? throw new Exception();
    if (!added.TryAdd(name, true)) return;

    slnproj.AddReference(ass, AddReferenceOptions.Default | AddReferenceOptions.HidePrivate);
  }
  
  private Task AddCentazioNuGetReferencesToProject(Dictionary<string, bool> added) {
    return AddLatestNuGetReferencesToProject([
      "Serilog",
      "Serilog.Sinks.Console",
      "Serilog.Extensions.Logging",
      "System.ClientModel" // not required but avoids versioning warnings
    ], added);
  }

  private void AddSettingsFilesToProject() {
    var files = new SettingsLoader().GetSettingsFilePathList(environment);
    AddCopyFilesToProject(files);
  }

  private void AddSecretsFilesToProject() {
    var path = new SecretsFileLoader(settings.GetSecretsFolder()).GetSecretsFilePath(environment);
    AddCopyFilesToProject([path]);
  }
  
  private void AddCopyFilesToProject(List<string> files) {
    files.ForEach(path => {
      var fname = Path.GetFileName(path);
      slnproj.AddItem("None", fname, [new("CopyToOutputDirectory", "PreserveNewest")]);
      File.Copy(path, Path.Combine(slnproj.ProjectPath, fname), true);
    });
  }
  
  protected async Task AddLatestNuGetReferencesToProject(List<string> packages, Dictionary<string, bool> added) => (await NugetHelpers.GetLatestStableVersions(packages))
      .ForEach(p => {
        if (!added.TryAdd(p.name, true)) return;
        slnproj.AddPackageReference(p.name, p.version);
      });
  
  protected abstract Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added);
}