using System.ComponentModel;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Settings;
using Centazio.Hosts.Self;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Host;

public class RunHostCommand(CentazioSettings settings, SelfHost host) : AbstractCentazioCommand<RunHostCommand.Settings>{

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings {
    AssemblyNames = UiHelpers.Ask("Assembly Names (comma separated)"),
    FunctionFilter = UiHelpers.Ask("Function Filter", "All") 
  });

  public override async Task ExecuteImpl(Settings cmdsetts) => await host.Run(settings, cmdsetts);

  public class Settings : CommonSettings, IHostConfiguration {
    [CommandArgument(0, "[ASSEMBLY_NAME]")] public required string AssemblyNames { get; init; }
    [CommandArgument(1, "[FUNCTION_FILTER]"), DefaultValue("All")] public string FunctionFilter { get; init; } = "All";
    [CommandOption("-q|--quiet")] public bool Quiet { get; init; } = false;
    [CommandOption("-f|--show-flows-only")] public bool FlowsOnly { get; init; } = false;
  }
}