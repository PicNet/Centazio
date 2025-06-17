using Centazio.Core.Secrets;
using Centazio.Providers.Az.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Az.Tests.Secrets;

public class AzSecretsLoaderTests: BaseSecretsLoaderTests {

  protected override async Task<ISecretsLoader> GetSecretsLoader() => 
      new AzSecretsLoaderFactory(await F.Settings()).GetService();

  // todo: implement
  protected override Task PrepareTestEnvironment(string environment, string contents) => Task.CompletedTask;

}