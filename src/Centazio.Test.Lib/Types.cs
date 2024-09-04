namespace Centazio.Test.Lib;

public record TestSettingsRaw {
  public string? SecretsFolder { get; init; }
  
  public static explicit operator TestSettings(TestSettingsRaw raw) => new(
      raw.SecretsFolder ?? throw new ArgumentNullException(nameof(SecretsFolder)));
}

public record TestSettings(string SecretsFolder);

public record TestSecretsRaw {
  public string? AWS_KEY { get; init; }
  public string? AWS_SECRET { get; init; }
  public string? SQL_CONN_STR { get; init; }
  
  public static explicit operator TestSecrets(TestSecretsRaw raw) => new(
      raw.AWS_KEY ?? throw new ArgumentNullException(nameof(AWS_KEY)),
      raw.AWS_SECRET ?? throw new ArgumentNullException(nameof(AWS_SECRET)),
      raw.SQL_CONN_STR ?? throw new ArgumentNullException(nameof(SQL_CONN_STR)));
}
public record TestSecrets(string AWS_KEY, string AWS_SECRET, string SQL_CONN_STR);