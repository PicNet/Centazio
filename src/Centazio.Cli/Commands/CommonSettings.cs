using Centazio.Core;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommonSettings : CommandSettings {
  
  [CommandOption("-e|--env <ENVIRONMENT>")]
  [DefaultValue(new [] { CentazioConstants.DEFAULT_ENVIRONMENT })]
  // todo: do we really need EnvironmentList?
  public string[] Environments { protected get; set; } = null!;
  public List<string> EnvironmentsList => Environments.ToList();
  
  internal Task SetInteractiveCommonOpts() {
    Environments = UiHelpers.AskForArr("Environment (csv)", [CentazioConstants.DEFAULT_ENVIRONMENT]);
    return Task.CompletedTask;
  }
}