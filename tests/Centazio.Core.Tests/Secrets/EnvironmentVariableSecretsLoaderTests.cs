using Centazio.Core.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

public class EnvironmentVariableSecretsLoaderTests : BaseSecretsLoaderTests {

  [SetUp] public void Setup() { }
  protected override async Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs) {
    envs.ToList().ForEach(f => {
      var prefix = $"{f.env.ToUpper()}_";
      f.contents.Split('\n').ForEach(l => {
        var index = l.Trim().IndexOf('=');
        if (index < 0) return;

        var key = l.Trim().Substring(0, index);
        var value = l.Trim().Substring(index + 1);
        Environment.SetEnvironmentVariable(prefix + key, value);
      });
    });
    return (TestSettingsTargetObj) await new EnvironmentSecretsLoader().Load<TestSettingsTargetObjRaw>(envs.Select(e => e.env).ToList());
  }

}