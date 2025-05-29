using Centazio.Core.Runner;
using Centazio.Core.Settings;

namespace Centazio.Cli.Commands.Gen.Cloud;

internal class AzCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, AzFunctionProjectMeta project, List<string> environments) : 
    CloudSolutionGenerator(settings, templater, project, typeof(Hosts.Az.AzHost).Assembly, environments, null) {

  protected override async Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added) {
    await AddAzNuGetReferencesToProject(added);
    await AddAzConfigJsonFilesToProject();
    await AddAzFunctionsToProject(functions);
  }

  private Task AddAzNuGetReferencesToProject(Dictionary<string, bool> added) =>
      AddLatestNuGetReferencesToProject([
        "Microsoft.Azure.Functions.Worker",
        "Microsoft.Azure.Functions.Worker.Extensions.Timer",
        "Microsoft.Azure.Functions.Worker.Sdk",
        "Microsoft.ApplicationInsights.WorkerService",
        "Microsoft.Azure.Functions.Worker.ApplicationInsights",
        "Serilog.Sinks.ApplicationInsights",
        "Microsoft.Extensions.Logging",
        "Serilog.Extensions.Hosting"
      ], added);
  
  private async Task AddAzConfigJsonFilesToProject() {
    await AddTemplateFileToProject("host.json");
    await AddTemplateFileToProject("local.settings.json");
    
    async Task AddTemplateFileToProject(string fname) {
      model.Files.Add(fname);
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, fname), templater.ParseFromPath($"azure/{fname}" ));
    }
  }
  
  private async Task AddAzFunctionsToProject(List<Type> functions) {
    var environments = GetEnvironmentsArrayString();
    await functions.ForEachSequentialAsync(async func => {
      var impl = IntegrationsAssemblyInspector.CreateFuncWithNullCtorArgs(func);
      var clcontent = templater.ParseFromPath("azure/function.cs", new {
        ClassName = func.Name,
        ClassFullName = func.FullName,
        FunctionNamespace = func.Namespace, 
        NewAssemblyName = project.ProjectName,
        Environments = environments,
        FunctionTimerCronExpr = impl.GetFunctionPollCronExpression(settings.Defaults)
      });
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"{func.Name}Azure.cs"), clcontent);
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"Program.cs"), templater.ParseFromPath("azure/function_app_program.cs", new { 
        Environments = environments,
        FunctionTypesListStr = $"new List<Type> {{{String.Join(", ", functions.Select(t => $"typeof({t})"))} }}"
      }));
    });
  }

}