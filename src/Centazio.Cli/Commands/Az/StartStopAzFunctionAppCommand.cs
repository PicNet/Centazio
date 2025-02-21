using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class StartStopAzFunctionAppCommand(CentazioSettings coresettings, ICommandRunner cmd) : AbstractCentazioCommand<StartStopAzFunctionAppCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    if (name != "start" && name != "stop") throw new ArgumentException($"only start/stop command is supported");
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.Defaults.GeneratedCodeFolder);
    
    if (name == "start") {
      await UiHelpers.Progress($"Starting Azure Function App '{project.DashedProjectName}'", async () => {
        await Task.Run(() => cmd.Az(coresettings.Parse(coresettings.Defaults.ConsoleCommands.Az.StartFunctionApp, new { AppName = project.DashedProjectName }), quiet: true));
        AnsiConsole.WriteLine($"Azure Function App '{project.DashedProjectName}' successfully started.");
      });
    } else if (name == "stop") {
      if (!UiHelpers.Confirm($"Are you sure you want to stop Azure Function App '{project.DashedProjectName}'")) {
        AnsiConsole.WriteLine("Aborting, no function stopped");
        return;
      }
      await UiHelpers.Progress($"Stopping Azure Function App '{project.DashedProjectName}'", async () => {
        await Task.Run(() => cmd.Az(coresettings.Parse(coresettings.Defaults.ConsoleCommands.Az.StopFunctionApp, new { AppName = project.DashedProjectName }), quiet: true));
        AnsiConsole.WriteLine($"Azure Function App '{project.DashedProjectName}' successfully stopped.");
      });
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
  }
}
