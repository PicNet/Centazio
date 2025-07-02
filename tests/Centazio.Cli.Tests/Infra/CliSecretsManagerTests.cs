using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Centazio.Providers.Az.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Tests.Infra;

public class CliSecretsManagerTests {

  [Test] public async Task Test_loading_File_secrets_loader() {
    var cm = await LoadSecretsManager();
    var settings = new CentazioSettings.Dto { SecretsLoaderSettings = new SecretsLoaderSettings.Dto { Provider = "File", SecretsFolder = "../centazio_3_secrets" } }.ToBase();
    var loader = cm.GetSecretsLoader(settings);
    Assert.That(loader is FileSecretsLoader);
  }

  [Test] public async Task Test_loading_Aws_secrets_loader() {
    var cm = await LoadSecretsManager();
    var settings = cm.GetSettingsFromKey(CentazioConstants.Hosts.Aws);
    var loader = cm.GetSecretsLoader(settings);
    
    Assert.That(settings.SecretsLoaderSettings.Provider.ToLower(), Is.EqualTo(CentazioConstants.Hosts.Aws));
    Assert.That(loader is AwsSecretsLoader);
  }
  
  [Test] public async Task Test_loading_Azure_secrets_loader() {
    var cm = await LoadSecretsManager();
    var settings = new CentazioSettings.Dto {
      AzureSettings = new AzureSettings.Dto {
        Region = nameof(AzureSettings.Dto.Region),
        ResourceGroup = nameof(AzureSettings.Dto.ResourceGroup),
        KeyVaultName = nameof(AzureSettings.Dto.KeyVaultName),
      },
      SecretsLoaderSettings = new SecretsLoaderSettings.Dto { Provider = "Az" } 
    }.ToBase();
    var loader = cm.GetSecretsLoader(settings);
    
    Assert.That(loader is AzSecretsLoader);
  }
  
  private static async Task<CliSecretsManager> LoadSecretsManager() {
    var svcs = new ServiceCollection();
    var conf = new SettingsLoaderConfig();
      
    SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT), svcs, String.Empty);
    SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Aws), svcs, CentazioConstants.Hosts.Aws);
    SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Az), svcs, CentazioConstants.Hosts.Az);
    svcs.AddSingleton<ICliSecretsManager>(prov => new CliSecretsManager(prov));
    var prov = svcs.BuildServiceProvider();
    var cm = (CliSecretsManager) prov.GetRequiredService<ICliSecretsManager>();
    return cm;
  }

}