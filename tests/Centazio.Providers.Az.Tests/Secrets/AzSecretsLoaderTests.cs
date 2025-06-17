using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Providers.Az.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Az.Tests.Secrets;

public class AzSecretsLoaderTests: BaseSecretsLoaderTests {

  protected override async Task<ISecretsLoader> GetSecretsLoader() => 
      new AzSecretsLoaderFactory(await F.Settings()).GetService();

  protected override async Task PrepareTestEnvironment(string environment, Dictionary<string, string> secrets) {
    var settings = await F.Settings();
    var name = settings.AzureSettings.GetKeySecretNameForEnvironment(environment);
    await AzSecretsLoader.GetSecretsClient(settings).SetSecretAsync(name, Json.Serialize(secrets));
  }

}