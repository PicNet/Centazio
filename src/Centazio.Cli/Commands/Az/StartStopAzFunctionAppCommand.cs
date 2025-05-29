using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class StartStopAzFunctionAppCommand([FromKeyedServices("az")] CentazioSettings coresettings, ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<StartStopAzFunctionAppCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  public override async Task ExecuteImpl(Settings settings) {
    if (CommandName != "start" && CommandName != "stop") throw new ArgumentException($"only start/stop command is supported");
    var project = new AzFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings, templater);
    
    if (CommandName == "start") {
      await UiHelpers.Progress($"Starting Azure Function App '{project.DashedProjectName}'", async () => {
        await Task.Run(() => cmd.Az(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Az.StartFunctionApp, new { AppName = project.DashedProjectName }), quiet: true));
        UiHelpers.Log($"Azure Function App '{project.DashedProjectName}' successfully started.");
      });
    } else if (CommandName == "stop") {
      if (!UiHelpers.Confirm($"Are you sure you want to stop Azure Function App '{project.DashedProjectName}'")) {
        UiHelpers.Log("Aborting, no function stopped", LogEventLevel.Warning);
        return;
      }
      await UiHelpers.Progress($"Stopping Azure Function App '{project.DashedProjectName}'", async () => {
        await Task.Run(() => cmd.Az(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Az.StopFunctionApp, new { AppName = project.DashedProjectName }), quiet: true));
        UiHelpers.Log($"Azure Function App '{project.DashedProjectName}' successfully stopped.");
      });
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
  }
}
