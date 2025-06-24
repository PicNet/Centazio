using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class GenerateAwsFunctionsCommand([FromKeyedServices(CentazioConstants.Hosts.Aws)] CentazioSettings coresettings, ITemplater templater) : AbstractCentazioCommand<GenerateAwsFunctionsCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() {
    var assembly = UiHelpers.Ask("Assembly Name");
    var settings = new Settings { 
      AssemblyName = assembly,
      FunctionName = AwsCommandsHelpers.GetTargetFunction(assembly) 
    }; 
    return Task.FromResult(settings);
  }

  public override async Task ExecuteImpl(Settings settings) {
    var project = new AwsFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings, settings.FunctionName, settings.EnvironmentsList);
    
    await UiHelpers.Progress("Generating AWS Lambda Function project", async () => await new AwsCloudSolutionGenerator(coresettings, templater, project, settings.EnvironmentsList, settings.FunctionName).GenerateSolution());
    await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
    [CommandArgument(1, "<FUNCTION_NAME>")] public required string FunctionName { get; init; }
  }

}