using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

internal class AwsCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta project, string environment) : CloudSolutionGenerator(project, environment) {
  
  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj) => new AwsCloudProjectGenerator(settings, templater, project, proj, environment);

  internal class AwsCloudProjectGenerator(CentazioSettings settings, ITemplater templater, FunctionProjectMeta projmeta, IXProject slnproj, string environment) : AbstractCloudProjectGenerator(settings, projmeta, slnproj, environment) {

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
        slnproj.AddItem("None", $"aws-lambda-tools-defaults-{func.Name}.json", [new("CopyToOutputDirectory", "PreserveNewest")]);
      
        // todo: this will have to be renamed during deploy time to deploy correct lambda
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"aws-lambda-tools-defaults-{func.Name}.json"), templater.ParseFromPath("aws/aws-lambda-tools-defaults.json", new {
          ClassName = func.Name,
          AssemblyName = slnproj.ProjectName, 
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
          NewAssemblyName = slnproj.ProjectName,
          Environment = environment
        });
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"{func.Name}Handler.cs"), handlerContent);
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, "Program.cs"), templater.ParseFromPath("aws/lambda_program.cs", new { 
          ClassName = func.Name, 
          NewAssemblyName = slnproj.ProjectName 
        }));
      });
    }
  }
}