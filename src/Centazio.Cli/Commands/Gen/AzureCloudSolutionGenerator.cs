using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

internal class AzureCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta project, string environment) : CloudSolutionGenerator(project, environment) {

  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj) => new AzureCloudProjectGenerator(settings, templater, project, proj, environment);

  internal class AzureCloudProjectGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta projmeta, IXProject slnproj, string environment) : AbstractCloudProjectGenerator(settings, projmeta, slnproj, environment) {

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
        slnproj.AddItem("None", fname, [new("CopyToOutputDirectory", "PreserveNewest")]);
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, fname), templater.ParseFromPath($"azure/{fname}"));
      }
    }
  
    private async Task AddAzureFunctionsToProject(List<Type> functions) {
      await functions.ForEachSequentialAsync(async func => {
        var clcontent = templater.ParseFromPath("azure/function.cs", new {
          ClassName=func.Name,
          ClassFullName=func.FullName,
          FunctionNamespace=func.Namespace, 
          NewAssemblyName=slnproj.ProjectName,
          Environment=environment
        });
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"{func.Name}.cs"), clcontent);
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"Program.cs"), templater.ParseFromPath("azure/function_app_program.cs"));
      });
    }

  }

}