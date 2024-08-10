namespace centazio.core.tests;

public record TestSettings(string SecretsFolder) {
  public TestSettings() : this("") {}
}

public record TestSecrets(string AWS_KEY, string AWS_SECRET) {
  public TestSecrets() : this("", "") {}
}