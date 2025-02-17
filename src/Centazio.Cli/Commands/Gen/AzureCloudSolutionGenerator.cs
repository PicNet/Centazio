using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

internal class AzureCloudSolutionGenerator(CentazioSettings settings, FunctionProjectMeta project, string environment) : CloudSolutionGenerator(project, environment) {

  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj) => new AzureCloudProjectGenerator(settings, project, proj, environment);

  internal class AzureCloudProjectGenerator(CentazioSettings settings, FunctionProjectMeta projmeta, IXProject slnproj, string environment) : AbstractCloudProjectGenerator(settings, projmeta, slnproj, environment) {

    protected override async Task AddCloudSpecificContentToProject(List<Type> functions) {
      await AddAzureNuGetReferencesToProject();
      await AddAzConfigJsonFilesToProject();
      await AddAzureFunctionsToProject(functions);
    }
  
    private Task AddAzureNuGetReferencesToProject() => 
        AddLatestNuGetReferencesToProject([
          "Microsoft.Azure.Functions.Worker",
          "Microsoft.Azure.Functions.Worker.Extensions.Timer",
          "Microsoft.Azure.Functions.Worker.Sdk"
        ]);
    
    private async Task AddAzConfigJsonFilesToProject() {
      await AddTemplateFileToProject("host.json");
      await AddTemplateFileToProject("local.settings.json");
      
      async Task AddTemplateFileToProject(string fname) {
        slnproj.AddItem("None", fname, [new("CopyToOutputDirectory", "PreserveNewest")]);
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, fname), settings.Template($"azure/{fname}"));
      }
    }
  
    private async Task AddAzureFunctionsToProject(List<Type> functions) {
      await functions.ForEachSequentialAsync(async func => {
        var clcontent = settings.Template("azure/function.cs", new {
          ClassName=func.Name, 
          FunctionNamespace=func.Namespace, 
          NewAssemblyName=slnproj.ProjectName,
          Environment=environment
        });
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"{func.Name}.cs"), clcontent);
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"Program.cs"), settings.Template("azure/function_app_program.cs"));
      });
    }

  }

}