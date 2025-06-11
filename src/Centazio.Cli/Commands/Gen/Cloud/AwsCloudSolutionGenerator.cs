using Centazio.Core.Runner;
using Centazio.Core.Settings;

namespace Centazio.Cli.Commands.Gen.Cloud;

internal class AwsCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, AwsFunctionProjectMeta project, List<string> environments, string funcname) : 
    CloudSolutionGenerator(settings, templater, project, typeof(Hosts.Aws.AwsHost).Assembly, environments, funcname) {

  protected override async Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added) {
    await AddAwsNuGetReferencesToProject(added);
    await AddAwsConfigFilesToProject(functions);
    await AddAwsFunctionsToProject(functions);
  }

  private Task AddAwsNuGetReferencesToProject(Dictionary<string, bool> added) =>
    AddLatestNuGetReferencesToProject([
      "Amazon.Lambda.Core",
      "Amazon.Lambda.Serialization.SystemTextJson",
      "Amazon.Lambda.RuntimeSupport",
      "AWSSDK.SecretsManager",
      "AWSSDK.SecretsManager.Caching",
      "AWSSDK.Core",
      "Amazon.Extensions.Configuration.SystemsManager",
      "AWSSDK.SSO",
      "AWSSDK.SSOOIDC",
    ], added);
  
  private async Task AddAwsConfigFilesToProject(List<Type> functions) {
    await functions.ForEachSequentialAsync(async func => {
      model.Files.Add($"Dockerfile");
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"Dockerfile"), templater.ParseFromPath("aws/Dockerfile", new {
        ClassName = func.Name,
        AssemblyName = func.Namespace,
        FileName = $"{func.Namespace}.{func.Name}.Aws" // todo CP: get the file name properly without replace
      }));
    });
  }

  private async Task AddAwsFunctionsToProject(List<Type> functions) {
    var environments = GetEnvironmentsArrayString(); 
    await functions.ForEachSequentialAsync(async func => {
      var impl = IntegrationsAssemblyInspector.CreateFuncWithNullCtorArgs(func);
      var handlerContent = templater.ParseFromPath("aws/function.cs", new {
        ClassName = func.Name,
        ClassFullName = func.FullName,
        FunctionNamespace = func.Namespace,
        Environments = environments,
        FunctionTimerCronExpr = impl.GetFunctionPollCronExpression(settings.Defaults)
      });
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"{func.Name}Handler.cs"), handlerContent);
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, "Program.cs"), templater.ParseFromPath("aws/lambda_program.cs", new {
        Environments = environments,
        ClassName = func.Name, 
        FunctionNamespace = func.Namespace,
      }));
    });
  }
}
