using System.Reflection;
using Centazio.Core;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using ReflectionUtils = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Cli.Commands.Gen.Cloud;

public abstract class CloudSolutionGenerator(
    CentazioSettings settings,
    ITemplater templater,
    AbstractFunctionProjectMeta project,
    Assembly hostass,
    List<string> environments,
    string? funcname) {

  protected CsProjModel model = null!;
  protected readonly ITemplater templater = templater;
  protected readonly CentazioSettings settings = settings;
  
  public async Task GenerateSolution() {
    if (Directory.Exists(project.SolutionDirPath)) Directory.Delete(project.SolutionDirPath, true);
    Directory.CreateDirectory(project.SolutionDirPath);
    
    model = new CsProjModel(project.ProjectName);
    await GenerateSlnFile();
    await GenerateProjects();
  }

  private async Task GenerateSlnFile() {
    var contents = templater.ParseFromPath("Solution.sln", new { Projects = new [] { model } });
    await File.WriteAllTextAsync(project.SlnFilePath, contents);
  }

  public async Task GenerateProjects() {
    model.GlobalProperties.AddRange([
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
      
    var functions = IntegrationsAssemblyInspector.GetCentazioFunctions(project.Assembly, funcname == null ? [] :[funcname]);
    await AddCloudSpecificContentToProject(functions, added);
    
    var contents = templater.ParseFromPath("Project.csproj", model);
    await File.WriteAllTextAsync(project.CsprojPath, contents);
  }

  private void AddCentazioProjectReferencesToProject(Dictionary<string, bool> added) {
    AddReferenceIfRequired(typeof(AbstractFunction<>).Assembly, added); // Add Centazio.Core
    AddReferenceIfRequired(project.Assembly, added); // Add this function's assemply
    AddReferenceIfRequired(hostass, added); // Add the hosting assembly (from Centazio.Hosts)
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

    model.AssemblyReferences.Add(new (ass.FullName ?? throw new Exception(), ass.Location));
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
    var files = new SettingsLoader().GetSettingsFilePathList(environments.AddIfNotExists(project.CloudName.ToLower()));
    AddCopyFilesToProject(files.Where(f => f.Contains("defaults")).ToList(), "defaults");
    AddCopyFilesToProject(files.Where(f => !f.Contains("defaults")).ToList(), String.Empty);
  }

  private void AddSecretsFilesToProject() {
    var loader = new FileSecretsLoader(settings.GetSecretsFolder());
    var paths = environments.AddIfNotExists(project.CloudName.ToLower()).Select((env, idx) => loader.GetSecretsFilePath(env, idx == 0)).OfType<string>().ToList();
    AddCopyFilesToProject(paths, String.Empty);
  }
  
  private void AddCopyFilesToProject(List<string> files, string subdir) {
    var targetDir = Path.Combine(project.ProjectDirPath, subdir);
    if (!File.Exists(targetDir)) { Directory.CreateDirectory(targetDir); }

    files.ForEach(path => {
      var fname = Path.GetFileName(path);
      model.Files.Add(string.IsNullOrEmpty(subdir) ? fname : Path.Combine(subdir, fname));
      File.Copy(path, Path.Combine(targetDir, fname), true);
    });
  }
  
  protected async Task AddLatestNuGetReferencesToProject(List<string> packages, Dictionary<string, bool> added) =>
      (await NugetHelpers.GetLatestStableVersions(packages))
      .ForEach(p => {
        if (!added.TryAdd(p.name, true)) return;
        model.NuGetReferences.Add(new(p.name, p.version));
      });
  
  protected string GetEnvironmentsArrayString() {
    var envs = environments.AddIfNotExists(project.CloudName.ToLower());
    return $"[\"{String.Join("\",\"", envs)}\"]";
  }
  
  protected abstract Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added);
}