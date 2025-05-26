using System.ComponentModel;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Settings;
using Centazio.Hosts.Aws;
using Centazio.Hosts.Self;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AwsFunctionLocalTestCommand(CentazioSettings settings, SelfHost host) : AbstractCentazioCommand<AwsFunctionLocalTestCommand.Settings> {
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings {
    AssemblyNames = UiHelpers.Ask("Assembly Names (comma separated)"),
    FunctionFilter = UiHelpers.Ask("Function Filter", "All")
  });

  public override async Task ExecuteImpl(Settings cmdsetts) {
    cmdsetts.EnvironmentsList.AddIfNotExists("aws");
    cmdsetts.EnvironmentsList.AddIfNotExists(nameof(SelfHost).ToLower());
    await host.RunHost(settings, cmdsetts, new AwsHostCentazioEngineAdapter(cmdsetts.EnvironmentsList, cmdsetts.UseLocalAws));
  }
  
  public class Settings : CommonSettings, IHostConfiguration {
    [CommandArgument(0, "[ASSEMBLY_NAME]")] public required string AssemblyNames { get; init; }
    [CommandArgument(1, "[FUNCTION_FILTER]"), DefaultValue("All")] public string FunctionFilter { get; init; } = "All";
    [CommandOption("-q|--quiet")] public bool Quiet { get; init; } = false;
    [CommandOption("-f|--show-flows-only")] public bool FlowsOnly { get; init; } = false;
    [CommandOption("-l|--local")] [DefaultValue(false)] public bool UseLocalAws { get; set; }
  }
}
