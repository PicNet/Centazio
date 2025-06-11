using Centazio.Core.Secrets;
using Centazio.Providers.Az.Secrets;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Az.Tests.Secrets;

[Ignore("Defaulted to aws secretloader for build agent")]
public class AzSecretsLoaderTests: BaseSecretsLoaderTests {

  private ISecretsLoader loader;

  [SetUp] public void Setup() {
    var settings = TestingFactories.Settings().Result;
    
    loader = new AzSecretsLoaderFactory(settings).GetService();
  }

  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    return (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }
  
}