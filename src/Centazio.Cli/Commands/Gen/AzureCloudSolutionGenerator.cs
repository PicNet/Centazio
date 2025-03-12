using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Cli.Commands.Gen;

internal class AzureCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, AzureFunctionProjectMeta project, string environment) : CloudSolutionGenerator(settings, templater, project, environment) {

  protected override async Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added) {
    await AddAzureNuGetReferencesToProject(added);
    await AddAzConfigJsonFilesToProject();
    await AddAzureFunctionsToProject(functions);
  }

  private Task AddAzureNuGetReferencesToProject(Dictionary<string, bool> added) => 
      AddLatestNuGetReferencesToProject([
        "Microsoft.Azure.Functions.Worker",
        "Microsoft.Azure.Functions.Worker.Extensions.Timer",
        "Microsoft.Azure.Functions.Worker.Sdk"
      ], added);
  
  private async Task AddAzConfigJsonFilesToProject() {
    await AddTemplateFileToProject("host.json");
    await AddTemplateFileToProject("local.settings.json");
    
    async Task AddTemplateFileToProject(string fname) {
      model.Files.Add(fname);
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, fname), templater.ParseFromPath($"azure/{fname}"));
    }
  }

  private async Task AddAzureFunctionsToProject(List<Type> functions) {
    await functions.ForEachSequentialAsync(async func => {
      var clcontent = templater.ParseFromPath("azure/function.cs", new {
        ClassName=func.Name,
        ClassFullName=func.FullName,
        FunctionNamespace=func.Namespace, 
        NewAssemblyName = project.ProjectName,
        Environment=environment
      });
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"{func.Name}.cs"), clcontent);
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"Program.cs"), templater.ParseFromPath("azure/function_app_program.cs"));
    });
  }

}