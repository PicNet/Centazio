using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployAzFunctionsCommand([FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSettings coresettings,  IAzFunctionDeployer impl, ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<DeployAzFunctionsCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  public override async Task ExecuteImpl(Settings settings) {
    var project = new AzFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings, templater);
    
    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await new AzCloudSolutionGenerator(coresettings, templater, project, settings.EnvironmentsList).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
    
    await UiHelpers.Progress($"Deploying the Azure Function '{project.DashedProjectName}'", async () => await impl.Deploy(project));
    UiHelpers.Log($"Azure Function '{project.DashedProjectName}' deployed.");
    
    if (settings.ShowLogs) {
      UiHelpers.Log($"Attempting to connect to function log stream.");
      cmd.Func(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Func.ShowLogStream, new { AppName = project.DashedProjectName }));
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
    [CommandOption("-l|--show-logs")] [DefaultValue(false)] public bool ShowLogs { get; set; }
  }
}
