using Centazio.Core.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

public class FileSecretsLoaderTests : BaseSecretsLoaderTests {

  [SetUp] public void Setup() { }
  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    envs.ToList().ForEach(f => File.WriteAllText($"{f.env}.env", f.contents));
    try { return (TestSettingsTargetObj) await new FileSecretsLoader(".").Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList()); } 
    finally { envs.ToList().ForEach(f => File.Delete($"{f.env}.env")); }
  }

}