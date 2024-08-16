using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommonSettings : CommandSettings {
  [CommandOption("-e|--env <ENVIRONMENT>")] public string Env { get; set; } = null!;
}