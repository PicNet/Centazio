namespace Centazio.Cli;

public record CliSettings(string SecretsFolder, string DefaultResourceGroup) {
  public CliSettings() : this("", "") {}
}