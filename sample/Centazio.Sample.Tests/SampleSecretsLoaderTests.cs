using System.Text.RegularExpressions;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Sample.Shared;

namespace Centazio.Sample.Tests;

public class SampleSecretsLoaderTests {

  private readonly CentazioSettings settings = 
      new CentazioSettings.Dto { SecretsLoaderSettings = new SecretsLoaderSettings.Dto { Provider = "File", SecretsFolder = "../centazio3_secrets" } }.ToBase();
  
  [Test] public async Task Test_that_subclass_fields_are_available() {
    var secrets = await new FileSecretsLoader(settings).Load<Secrets>("dev");
    
    Assert.That(String.IsNullOrWhiteSpace(secrets.APPSHEET_KEY), Is.False);
    Assert.That(String.IsNullOrWhiteSpace(secrets.CLICKUP_TOKEN), Is.False);
  }
  
  [Test] public async Task Test_that_base_class_fields_are_available() {
    var secrets = await new FileSecretsLoader(settings).Load<Secrets>("dev");
    
    Assert.That(String.IsNullOrWhiteSpace(secrets.AWS_KEY), Is.False);
    Assert.That(String.IsNullOrWhiteSpace(secrets.AWS_SECRET), Is.False);
  }
  
  [Test] public async Task Test_that_required_fields_are_validated() {
    await CreateSettingsFileWithMissingSetting();
    Assert.ThrowsAsync<Exception>(async () => await new FileSecretsLoader(settings).Load<Secrets>("missing"));

    async Task CreateSettingsFileWithMissingSetting() {
      var contents = await File.ReadAllTextAsync(FsUtils.GetCentazioPath("../centazio3_secrets/dev.env"));
      var withmissing = Regex.Replace(contents, "APPSHEET_KEY=.*", String.Empty);
      var missingfile = FsUtils.GetCentazioPath("../centazio3_secrets/missing.env");
      await File.WriteAllTextAsync(missingfile, withmissing);
    }
  }
  
  [TearDown] public void TearDown() {
    var missingfile = FsUtils.GetCentazioPath("../centazio3_secrets/missing.env");
    if (File.Exists(missingfile)) File.Delete(missingfile);
  } 
}
