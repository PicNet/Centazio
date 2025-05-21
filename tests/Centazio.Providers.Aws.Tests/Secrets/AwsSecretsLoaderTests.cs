using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Tests.Secrets;
using Centazio.Providers.Aws.Secrets;

namespace Centazio.Providers.Aws.Tests.Secrets;
public class AwsSecretsLoaderTests : AbstractSecretsLoaderTests {

  private ISecretsLoader loader;

  [SetUp] public void Setup() {
    // todo: why do we not use the standard TestingFactories.Settings() here?
    var settings = new AwsSettings {
      Region = "ap-southeast-2",
      AccountName = "PicNet",
      SecretsManagerStoreIdTemplate = "picnet/centazio/<environment>"
    };
    
    loader = new AwsSecretsLoaderFactory(settings).GetService();
  }

  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    return (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }

}