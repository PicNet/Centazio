namespace Centazio.Cli;

public record CliSecrets(string AWS_KEY, string AWS_SECRET) {
  public CliSecrets() : this("", "") {}
}