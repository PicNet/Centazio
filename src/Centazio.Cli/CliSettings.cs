namespace Centazio.Cli;

public record CliSettings(string SecretsFolder, string DefaultAccountName, string DefaultResourceGroupName) {
  public CliSettings() : this("", "", "") {}
}