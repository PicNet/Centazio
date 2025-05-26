using System.ComponentModel;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Settings;
using Centazio.Hosts.Aws;
using Centazio.Hosts.Self;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Host;

public class RunHostCommand(CentazioSettings settings, SelfHost host, SelfAwsHost awsHost) : AbstractCentazioCommand<RunHostCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings {
    AssemblyNames = UiHelpers.Ask("Assembly Names (comma separated)"),
    FunctionFilter = UiHelpers.Ask("Function Filter", "All")
  });

  public override async Task ExecuteImpl(Settings cmdsetts) {
    // todo: remove, aws has nothing to do with SelfHost
    if (cmdsetts.UseAws) {
      cmdsetts.EnvironmentsList.AddIfNotExists("aws");
      var types = ((IHostConfiguration)cmdsetts).GetFunctions();
      await awsHost.RunAwsHost(settings, cmdsetts, new AwsHostCentazioEngineAdapter(cmdsetts.EnvironmentsList), cmdsetts.UseLocalAws);
    }
    else {
      cmdsetts.EnvironmentsList.AddIfNotExists(nameof(SelfHost).ToLower());
      await host.RunHost(settings, cmdsetts, new SelfHostCentazioEngineAdapter(settings, cmdsetts.EnvironmentsList));
    }
  }

  public class Settings : CommonSettings, IHostConfiguration {
    [CommandArgument(0, "[ASSEMBLY_NAME]")] public required string AssemblyNames { get; init; }
    [CommandArgument(1, "[FUNCTION_FILTER]"), DefaultValue("All")] public string FunctionFilter { get; init; } = "All";
    [CommandOption("-q|--quiet")] public bool Quiet { get; init; } = false;
    [CommandOption("-f|--show-flows-only")] public bool FlowsOnly { get; init; } = false;
    [CommandOption("--aws")] public bool UseAws { get; init; } = false;
    [CommandOption("--local-aws")] public bool UseLocalAws { get; init; } = false;
  }
}