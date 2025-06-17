using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Secrets;

public class FileSecretsLoaderTests : BaseSecretsLoaderTests {
  private readonly CentazioSettings settings = 
      new CentazioSettings.Dto { SecretsLoaderSettings = new SecretsLoaderSettings.Dto { Provider = "File", SecretsFolder = "../centazio3_secrets" } }.ToBase();
  
  protected override Task<ISecretsLoader> GetSecretsLoader() => 
      Task.FromResult(new FileSecretsLoaderFactory(settings).GetService());

  protected override async Task PrepareTestEnvironment(string environment, string contents) {
    await File.WriteAllTextAsync(Path.Join(settings.GetSecretsFolder(), environment + ".env"), contents);
  }
}