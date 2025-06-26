using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class ShowAzFunctionLogStreamCommand([FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSettings coresettings,  ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<ShowAzFunctionLogStreamCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  public override async Task ExecuteImpl(Settings settings) {
    var project = new AzFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings, templater);
    await cmd.Func(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Func.ShowLogStream, new { AppName = project.DashedProjectName }));
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
  }
}
