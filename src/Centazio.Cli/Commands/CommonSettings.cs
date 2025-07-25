﻿using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommonSettings : CommandSettings {
  
  [CommandOption("-e|--env <ENVIRONMENT>")]
  [DefaultValue(new [] { CentazioConstants.DEFAULT_ENVIRONMENT })]
  public string[] Environments { protected get; set; } = null!;
  public List<string> EnvironmentsList => Environments.ToList();
  
  internal Task SetInteractiveCommonOpts() {
    Environments = UiHelpers.AskForArr("Environment (csv)", [CentazioConstants.DEFAULT_ENVIRONMENT]);
    return Task.CompletedTask;
  }
}