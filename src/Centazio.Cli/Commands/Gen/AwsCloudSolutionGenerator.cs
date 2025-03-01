using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Cli.Commands.Gen;

internal class AwsCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta project, string environment) : CloudSolutionGenerator(templater, project, environment) {
  
  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(FunctionProjectMeta proj) => new AwsCloudProjectGenerator(settings, templater, project, environment);

  internal class AwsCloudProjectGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta project, string environment) : AbstractCloudProjectGenerator(settings, templater, project, environment) {

    protected override async Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added) {
      await AddAwsNuGetReferencesToProject(added);
      await AddAwsConfigFilesToProject(functions);
      await AddAwsFunctionsToProject(functions);
    }

    private Task AddAwsNuGetReferencesToProject(Dictionary<string, bool> added) => 
      AddLatestNuGetReferencesToProject([
        "Amazon.Lambda.Core",
        "Amazon.Lambda.Serialization.SystemTextJson",
        "Amazon.Lambda.RuntimeSupport"
      ], added);
    
    private async Task AddAwsConfigFilesToProject(List<Type> functions) {
      await functions.ForEachSequentialAsync(async func => {
        project.Files.Add($"aws-lambda-tools-defaults-{func.Name}.json");
      
        // todo: this will have to be renamed during deploy time to deploy correct lambda
        await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"aws-lambda-tools-defaults-{func.Name}.json"), templater.ParseFromPath("aws/aws-lambda-tools-defaults.json", new {
          ClassName = func.Name,
          AssemblyName = project.ProjectDirPath, 
          Environment = environment 
        }));
      });
    }
  
    private async Task AddAwsFunctionsToProject(List<Type> functions) {
      await functions.ForEachSequentialAsync(async func => {
        var handlerContent = templater.ParseFromPath("aws/function.cs", new {
          ClassName = func.Name,
          ClassFullName = func.FullName,
          FunctionNamespace = func.Namespace,
          NewAssemblyName = project.ProjectName,
          Environment = environment
        });
        await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"{func.Name}Handler.cs"), handlerContent);
        await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, "Program.cs"), templater.ParseFromPath("aws/lambda_program.cs", new { 
          ClassName = func.Name, 
          NewAssemblyName = project.ProjectName 
        }));
      });
    }
  }
}