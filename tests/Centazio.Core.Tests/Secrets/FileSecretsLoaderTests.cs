using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

// todo WT: failing test
public class FileSecretsLoaderTests : BaseSecretsLoaderTests {
  private ISecretsLoader loader;
  [SetUp] public void Setup() {
    var settings = new CentazioSettings.Dto { SecretsLoaderSettings = new SecretsLoaderSettings.Dto { Provider = "File", SecretsFolder = "../centazio3_secrets" } }.ToBase();
    loader = new FileSecretsLoaderFactory(settings).GetService();
  }
  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    return (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }

}