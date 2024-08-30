using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class TestingFactories {

  public static CentazioSecrets Secrets() {
    var settings = new SettingsLoader<TestSettings>().Load();
    return new NetworkLocationEnvFileSecretsLoader<CentazioSecrets>(settings.SecretsFolder, "dev").Load();
  }

}