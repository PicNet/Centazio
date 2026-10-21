namespace Centazio.Cli.Tests;

public class TestingCliSecretsManager : ICliSecretsManager {

  public static readonly ICliSecretsManager Instance = new TestingCliSecretsManager();

  public Task<T> LoadSecrets<T>(string settingskey, params List<string> environments) => F.Secrets<T>();

}