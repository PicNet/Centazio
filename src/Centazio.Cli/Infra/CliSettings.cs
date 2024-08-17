namespace Centazio.Cli.Infra;

public record CliSettings(string SecretsFolder, string DefaultAccountName, string DefaultResourceGroupName) {
  public CliSettings() : this("", "", "") {}
}