using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

public class FileSecretsLoaderTests : BaseSecretsLoaderTests {

  [SetUp] public void Setup() { }
  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    envs.ToList().ForEach(f => File.WriteAllText($"{f.env}.env", f.contents));
    var settings = new CentazioSettings.Dto { SecretsLoaderSettings = new SecretsLoaderSettings.Dto { Provider = "File", SecretsFolders = "." } }.ToBase();
    try { return (TestSettingsTargetObj) await new FileSecretsLoader(settings).Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList()); } 
    finally { envs.ToList().ForEach(f => File.Delete($"{f.env}.env")); }
  }

}