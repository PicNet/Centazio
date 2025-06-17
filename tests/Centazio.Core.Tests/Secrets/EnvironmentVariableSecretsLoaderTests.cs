using Centazio.Core.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

public class EnvironmentVariableSecretsLoaderTests : BaseSecretsLoaderTests {

  protected override Task PrepareTestEnvironment(string environment, Dictionary<string, string> secrets) {
    var prefix = $"{environment.ToUpper()}_";
    secrets.ForEach(kvp => Environment.SetEnvironmentVariable(prefix + kvp.Key, kvp.Value));
    return Task.CompletedTask;
  }

  protected override Task<ISecretsLoader> GetSecretsLoader() => Task.FromResult<ISecretsLoader>(new EnvironmentSecretsLoader());

}