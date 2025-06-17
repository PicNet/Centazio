using Centazio.Core.Secrets;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Aws.Tests.Secrets;
public class AwsSecretsLoaderTests : BaseSecretsLoaderTests {

  protected override async Task<ISecretsLoader> GetSecretsLoader() =>
    new AwsSecretsLoaderFactory(await F.Settings()).GetService();

  // todo: implement
  protected override Task PrepareTestEnvironment(string environment, string contents) => Task.CompletedTask;
}