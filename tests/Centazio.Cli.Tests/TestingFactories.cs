using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class TestingFactories {

  public static CentazioSecrets Secrets() {
    var settings = (TestSettings) new SettingsLoader<TestSettingsRaw>().Load("dev");
    return (CentazioSecrets) new NetworkLocationEnvFileSecretsLoader<CentazioSecrets.Dto>(settings.SecretsFolder, "dev").Load();
  }

}