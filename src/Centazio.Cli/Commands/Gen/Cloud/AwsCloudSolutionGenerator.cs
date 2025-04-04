﻿using Centazio.Core.Runner;
using Centazio.Core.Settings;

namespace Centazio.Cli.Commands.Gen.Cloud;

internal class AwsCloudSolutionGenerator(CentazioSettings settings, ITemplater templater, AwsFunctionProjectMeta project, List<string> environments) : CloudSolutionGenerator(settings, templater, project, environments) {

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
      model.Files.Add($"aws-lambda-tools-defaults.json");
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"aws-lambda-tools-defaults.json"), templater.ParseFromPath("aws/aws-lambda-tools-defaults.json", new {
        ClassName = func.Name,
        AssemblyName = project.ProjectDirPath 
      }));
    });
  }

  private async Task AddAwsFunctionsToProject(List<Type> functions) {
    await functions.ForEachSequentialAsync(async func => {
      var impl = IntegrationsAssemblyInspector.CreateFuncWithNullCtorArgs(func);
      var handlerContent = templater.ParseFromPath("aws/function.cs", new {
        ClassName = func.Name,
        ClassFullName = func.FullName,
        FunctionNamespace = func.Namespace,
        Environments = GetEnvironmentsArrayString(),
        FunctionTimerCronExpr = impl.GetFunctionPollCronExpression(settings.Defaults)
      });
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, $"{func.Name}Handler.cs"), handlerContent);
      await File.WriteAllTextAsync(Path.Combine(project.ProjectDirPath, "Program.cs"), templater.ParseFromPath("aws/lambda_program.cs", new { 
        ClassName = func.Name, 
        FunctionNamespace = func.Namespace,
      }));
    });
  }
}
