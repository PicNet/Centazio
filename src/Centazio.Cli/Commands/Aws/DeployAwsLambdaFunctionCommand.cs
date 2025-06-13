using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class DeployAwsLambdaFunctionCommand([FromKeyedServices(CentazioConstants.Hosts.Aws)] CentazioSettings coresettings,  IAwsFunctionDeployer impl, ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<DeployAwsLambdaFunctionCommand.Settings> {
  
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
    
    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Lambda Function project '{project.DashedProjectName}'", async () => await new AwsCloudSolutionGenerator(coresettings, templater, project, settings.EnvironmentsList, settings.FunctionName).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
    
    await UiHelpers.Progress($"Deploying the Lambda Function '{project.DashedProjectName}'", async () => await impl.Deploy(project));
    UiHelpers.Log($"Lambda Function '{project.DashedProjectName}' deployed.");
    
    if (settings.ShowLogs) {
      UiHelpers.Log($"Attempting to connect to function log stream.");
      await cmd.Func(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Lambda.ShowLogStream, new { AppName = project.DashedProjectName }));
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
    [CommandArgument(1, "<FUNCTION_NAME>")] public required string FunctionName { get; init; }
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
    [CommandOption("-l|--show-logs")] [DefaultValue(false)] public bool ShowLogs { get; set; }
  }
}
