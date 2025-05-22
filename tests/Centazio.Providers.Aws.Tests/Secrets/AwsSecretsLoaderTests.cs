using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Tests.Secrets;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib;

namespace Centazio.Providers.Aws.Tests.Secrets;
public class AwsSecretsLoaderTests : AbstractSecretsLoaderTests {

  private ISecretsLoader loader;

  [SetUp] public void Setup() {
    var settings = TestingFactories.Settings().Result.AwsSettings;
    
    loader = new AwsSecretsLoaderFactory(settings).GetService();
  }

  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    return (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }

}