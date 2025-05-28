using Centazio.Core.Secrets;
using Centazio.Providers.Az.Secrets;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Az.Tests.Secrets;

public class AzSecretsLoaderTests: BaseSecretsLoaderTests {

  private ISecretsLoader loader;

  [SetUp] public void Setup() {
    var settings = TestingFactories.Settings().Result;
    
    loader = new AzSecretsLoaderFactory(settings).GetService();
  }

  protected override async Task<BaseSecretsLoaderTests.TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    return (BaseSecretsLoaderTests.TestSettingsTargetObj) await loader.Load<BaseSecretsLoaderTests.TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }
  
}