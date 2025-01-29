using System.ComponentModel;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Settings;
using Centazio.Host;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Host;

public class RunHostCommand(CentazioSettings coresettings) : AbstractCentazioCommand<RunHostCommand.RunHostCommandSettings>{

  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new RunHostCommandSettings { FunctionFilter = UiHelpers.Ask("Function Filter", "all") });

  protected override async Task ExecuteImpl(RunHostCommandSettings cmdsetts) {
    ArgumentException.ThrowIfNullOrWhiteSpace(cmdsetts.FunctionFilter);
    await new CentazioHost(new HostSettings(cmdsetts, coresettings)).Run();
  }

  public class RunHostCommandSettings : CommonSettings, IHostConfiguration {
    [CommandArgument(0, "[FUNCTION_FILTER]"), DefaultValue("all")] public string FunctionFilter { get; init; } = "all";
    [CommandOption("-q|--quiet")] public bool Quiet { get; set; }
    [CommandOption("-f|--show-flows-only")] public bool FlowsOnly { get; set; }
  }
}