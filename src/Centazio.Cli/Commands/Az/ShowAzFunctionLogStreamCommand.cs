using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Misc;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class ShowAzFunctionLogStreamCommand(CentazioSettings coresettings,  ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<ShowAzFunctionLogStreamCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override Task ExecuteImpl(Settings settings) {
    var project = new AzureFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings.Defaults.GeneratedCodeFolder);
    cmd.Func(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Func.ShowLogStream, new { AppName = project.DashedProjectName }));
    return Task.CompletedTask;
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
  }
}
