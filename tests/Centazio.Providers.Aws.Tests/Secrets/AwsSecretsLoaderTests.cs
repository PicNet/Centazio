using Centazio.Core.Secrets;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Aws.Tests.Secrets;
public class AwsSecretsLoaderTests : BaseSecretsLoaderTests {

  private ISecretsLoader loader;

  [SetUp] public void Setup() {
    var settings = F.Settings().Result;
    
    loader = new AwsSecretsLoaderFactory(settings).GetService();
  }

  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    return (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }

}