namespace Centazio.Cli.Tests;

public class TestingCliSecretsManager : ICliSecretsManager {

  public static readonly ICliSecretsManager Instance = new TestingCliSecretsManager();

  public Task<T> LoadSecrets<T>(string settingskey) => F.Secrets<T>();

}