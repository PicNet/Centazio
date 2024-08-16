namespace Centazio.Cli;

public record CliSettings(string SecretsFolder, string DefaultAccountName) {
  public CliSettings() : this("", "") {}
}