using System.ComponentModel;
using Centazio.Cli.Infra.Ui;
using Centazio.Host;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Host;

// todo: support running functions from multiple assemblies
public class RunHostCommand(CentazioHost host) : AbstractCentazioCommand<RunHostCommand.Settings>{

  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new Settings {
        AssemblyName = UiHelpers.Ask("Assembly Name"),
        FunctionFilter = UiHelpers.Ask("Function Filter", "All") 
      });

  protected override async Task ExecuteImpl(Settings cmdsetts) => await host.Run(cmdsetts);

  public class Settings : CommonSettings, IHostConfiguration {
    [CommandArgument(0, "[ASSEMBLY_NAME]")] public string AssemblyName { get; init; } = null!;
    [CommandArgument(1, "[FUNCTION_FILTER]"), DefaultValue("All")] public string FunctionFilter { get; init; } = "All";
    [CommandOption("-q|--quiet")] public bool Quiet { get; set; }
    [CommandOption("-f|--show-flows-only")] public bool FlowsOnly { get; set; }
  }
}