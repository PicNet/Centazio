using System.ComponentModel;
using Centazio.Core;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommonSettings : CommandSettings {
  [CommandOption("-e|--env <ENVIRONMENT>")]
  [DefaultValue(CentazioConstants.DEFAULT_ENVIRONMENT)]
  public string Env { get; set; } = null!;
}