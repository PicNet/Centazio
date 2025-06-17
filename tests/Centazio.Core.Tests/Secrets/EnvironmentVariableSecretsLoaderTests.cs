using Centazio.Core.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

public class EnvironmentVariableSecretsLoaderTests : BaseSecretsLoaderTests {

  protected override Task PrepareTestEnvironment(string environment, string contents) {
    var prefix = $"{environment.ToUpper()}_";
    contents.Split('\n').ForEach(l => {
      var index = l.Trim().IndexOf('=');
      if (index < 0) return;

      var key = l.Trim()[..index];
      var value = l.Trim()[(index + 1)..];
      Environment.SetEnvironmentVariable(prefix + key, value);
    });
    return Task.CompletedTask;
  }

  protected override Task<ISecretsLoader> GetSecretsLoader() => Task.FromResult<ISecretsLoader>(new EnvironmentSecretsLoader());

}