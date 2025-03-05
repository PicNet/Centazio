﻿using System.ComponentModel;
using Centazio.Cli.Infra.Ui;
using Centazio.Host;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Host;

public class RunHostCommand(CentazioHost host) : AbstractCentazioCommand<RunHostCommand.Settings>{

  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings {
    AssemblyNames = UiHelpers.Ask("Assembly Names (comma separated)"),
    FunctionFilter = UiHelpers.Ask("Function Filter", "All") 
  });

  protected override async Task ExecuteImpl(string name, Settings cmdsetts) => await host.Run(cmdsetts);

  public class Settings : CommonSettings, IHostConfiguration {
    [CommandArgument(0, "[ASSEMBLY_NAME]")] public required string AssemblyNames { get; init; }
    [CommandArgument(1, "[FUNCTION_FILTER]"), DefaultValue("All")] public string FunctionFilter { get; init; } = "All";
    // todo: can these setters actually be removed?
    [CommandOption("-q|--quiet")] public bool Quiet { get; set; }
    [CommandOption("-f|--show-flows-only")] public bool FlowsOnly { get; set; }
  }
}