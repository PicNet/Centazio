using System.Reflection;
using Centazio.Cli.Infra;
using Centazio.Core;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using ReflectionUtils = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Cli.Commands.Gen;

public enum ECloudEnv { Azure = 1, Aws = 2 }

public abstract class CloudSolutionGenerator(ITemplater templater, FunctionProjectMeta project, string environment) {
  
  protected readonly ITemplater templater = templater;
  protected readonly FunctionProjectMeta project = project;
  protected readonly string environment = environment;
  
  public static CloudSolutionGenerator Create(CentazioSettings settings, ITemplater templater, FunctionProjectMeta project, string environment) {
    ValidateProjectAssemblyBeforeCodeGen(project.Assembly);
    switch (project.Cloud) {
      case ECloudEnv.Aws: return new AwsCloudSolutionGenerator(settings, templater, project, environment);
      case ECloudEnv.Azure: return new AzureCloudSolutionGenerator(settings, templater, project, environment);
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
    
    await GenerateSlnFile();
    await GenerateProjects();
  }

  private async Task GenerateSlnFile() {
    var contents = templater.ParseFromPath("Solution.sln", new { Projects = new [] { project } });
    await File.WriteAllTextAsync(project.SlnFilePath, contents);
  }

  private async Task GenerateProjects() {
    await GetCloudProjectGenerator(project).Generate();
  }

  protected abstract AbstractCloudProjectGenerator GetCloudProjectGenerator(FunctionProjectMeta proj);

}

public abstract class AbstractCloudProjectGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta project, string environment) {
  
  protected readonly CentazioSettings settings = settings;
  protected readonly ITemplater templater = templater;
  protected readonly FunctionProjectMeta project = project;
  protected readonly string environment = environment;

  public async Task Generate() {
    project.GlobalProperties.AddRange([
      new("TargetFramework", "net9.0"),
      new("OutputType", "Exe"),
      new("ImplicitUsings", "enable"),
      new("Nullable", "enable"),
      new("TreatWarningsAsErrors", "true"),
      new("EnforceCodeStyleInBuild", "true"),
      new("ManagePackageVersionsCentrally", "false")]);
    AddSettingsFilesToProject();
    AddSecretsFilesToProject();
    
    var added = new Dictionary<string, bool>();
    AddCentazioProjectReferencesToProject(added);
    await AddCentazioProvidersAndRelatedNugetsToProject(added);
    await AddCentazioNuGetReferencesToProject(added);
      
    var functions = IntegrationsAssemblyInspector.GetCentazioFunctions(project.Assembly, []);
    await AddCloudSpecificContentToProject(functions, added);
    
    var contents = templater.ParseFromPath("Project.csproj", project);
    await File.WriteAllTextAsync(project.CsprojPath, contents);
  }

  private void AddCentazioProjectReferencesToProject(Dictionary<string, bool> added) {
    AddReferenceIfRequired(typeof(AbstractFunction<>).Assembly, added); // Add Centazio.Core
    AddReferenceIfRequired(project.Assembly, added); // Add this function's assemply
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

    project.AssemblyReferences.Add(new (ass.FullName ?? throw new Exception(), ass.Location));
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
      project.Files.Add(fname);
      File.Copy(path, Path.Combine(project.ProjectDirPath, fname), true);
    });
  }
  
  protected async Task AddLatestNuGetReferencesToProject(List<string> packages, Dictionary<string, bool> added) =>
      (await NugetHelpers.GetLatestStableVersions(packages))
      .ForEach(p => {
        if (!added.TryAdd(p.name, true)) return;
        project.NuGetReferences.Add(new(p.name, p.version));
      });
  
  protected abstract Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added);
}