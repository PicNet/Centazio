using Centazio.Cli.Infra;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class TestingFactories {

  public static CliSecrets Secrets() {
    var settings = new SettingsLoader<TestSettings>().Load();
    return new NetworkLocationEnvFileSecretsLoader<CliSecrets>(settings.SecretsFolder, "dev").Load();
  }

}