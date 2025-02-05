using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using Azure;
using Azure.Core;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployAzFunctionsCommand(IAzFunctionDeployer impl) : AbstractCentazioCommand<DeployAzFunctionsCommand.Settings> {
  
  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new Settings { 
        AssemblyName = UiHelpers.Ask("Assembly Name"),
        FunctionName = UiHelpers.Ask("Function Class Name", "All"),
      });

  protected override async Task ExecuteImpl(Settings settings) {
    await impl.Deploy("appname", "proj path");

  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
    [CommandArgument(0, "<FUNCTION-NAME>")] public string? FunctionName { get; init; }
  }
}

public interface IAzFunctionDeployer {
  Task Deploy(string appname, string projpath);
}

public class AzFunctionDeployer(AzureSettings settings, CentazioSecrets secrets) : AbstractAzCommunicator(secrets), IAzFunctionDeployer {

  public async Task Deploy(string appname, string projpath) {
    var rg = GetClient().GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(Secrets.AZ_SUBSCRIPTION_ID, settings.ResourceGroup));

    var appres = await GetFunctionAppIfExists(rg, appname);

    if (appres is null) {
      Console.WriteLine($"Function App {appname} does not exist. Creating new app...");
      await CreateNewFunctionApp(rg, appname, settings.Region);
      // todo: can this be done without making additional calls?
      appres = await GetFunctionAppIfExists(rg, appname);
      if (appres is null) throw new Exception();
    }
    // todo: do we need this update?  Not just deploy?
    // Console.WriteLine($"Function App {appname} exists. Updating existing app...");
    // await UpdateExistingFunctionApp(appres, settings.AzureSettings.Region);

    await PublishFunctionApp(appres, appname, projpath);
  }

  private async Task<WebSiteResource?> GetFunctionAppIfExists(ResourceGroupResource rg, string appname) {
    try { return (await rg.GetWebSiteAsync(appname)).Value; }
    catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
  }

  private async Task CreateNewFunctionApp(ResourceGroupResource rg, string appname, string location) {
    var plandata = new AppServicePlanData(location) { Sku = new AppServiceSkuDescription { Name = "Y1", Tier = "Dynamic" }, Kind = "functionapp" };
    var appplan = (await rg.GetAppServicePlans().CreateOrUpdateAsync(WaitUntil.Completed, $"{appname}-plan", plandata)).Value;
    var appconf = CreateFunctionAppConfiguration(location, appplan.Id); 
    await rg.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, appname, appconf);

    Console.WriteLine($"Successfully created new Function App: {appname}");
  }

  private async Task UpdateExistingFunctionApp(WebSiteResource funcapp, string location) {
    var appsettings = await GetExistingAppSettings(funcapp);

    // Update Function App configuration
    var updatedConfig = new SiteConfigProperties {
      // FunctionRuntime = "dotnet-isolated",
      // UseNestedWebConfig = false,
      AppSettings = appsettings,
      NetFrameworkVersion = "v8.0",
      MinTlsVersion = "1.2"
      // Http20Enabled = true
    };

    // await funcapp.UpdateAsync(WaitUntil.Completed, updateData);
    await funcapp.UpdateAsync(new SitePatchInfo() { SiteConfig = updatedConfig });
    Console.WriteLine($"Successfully updated Function App: {funcapp.Data.Name}");
  }

  private async Task<List<AppServiceNameValuePair>> GetExistingAppSettings(WebSiteResource funcapp) => 
      (await funcapp.GetApplicationSettingsAsync()).Value.Properties.Select(kvp => new AppServiceNameValuePair { Name = kvp.Key, Value = kvp.Value }).ToList();

  private WebSiteData CreateFunctionAppConfiguration(string location, ResourceIdentifier farmid) => new(location) {
    Kind = "functionapp",
    SiteConfig = new SiteConfigProperties {
      // FunctionRuntime = "dotnet-isolated",
      // UseNestedWebConfig = false,
      NetFrameworkVersion = "v8.0",
      MinTlsVersion = "1.2",
      // Http20Enabled = true
    },
    AppServicePlanId = farmid, // is this correct instead of `ServerFarmId = farmid`
    // HttpsOnly = true
  };

  private async Task PublishFunctionApp(WebSiteResource appres, string functionAppName, string projpath) {
    try {
      var cred = await GetPublishCredentials(appres);
      var zippath = CreateFunctionAppZip(projpath);

      try {
        var endpoint = $"https://{appres.Data.DefaultHostName.Replace("azurewebsites.net", "scm.azurewebsites.net")}/api/zipdeploy";

        // Set up authentication
        var http = new HttpClient();
        var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cred.PublishingUserName}:{cred.PublishingPassword}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        var zipbytes = await File.ReadAllBytesAsync(zippath);
        using var content = new ByteArrayContent(zipbytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        Console.WriteLine("Starting ZIP deployment...");
        var response = await http.PostAsync(endpoint, content);

        if (!response.IsSuccessStatusCode) {
          var err = await response.Content.ReadAsStringAsync();
          throw new Exception($"Deployment failed. Status: {response.StatusCode}, Error: {err}");
        }

        // Stop and start the Function App to ensure changes are applied
        Console.WriteLine("Restarting Function App...");
        await appres.RestartAsync();

        Console.WriteLine("Deployment completed successfully");
      }
      finally {
        // Cleanup temporary zip file
        if (File.Exists(zippath)) File.Delete(zippath);
      }
    }
    catch (Exception ex) {
      throw new Exception($"Failed to publish Function App: {ex.Message}", ex);
    }
  }

  private async Task<PublishingUserData> GetPublishCredentials(WebSiteResource appres) => 
      (await appres.GetPublishingCredentialsAsync(WaitUntil.Completed)).Value.Data;

  private string CreateFunctionAppZip(string projectPath) {
    var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
    ZipFile.CreateFromDirectory(projectPath, zipPath);
    return zipPath;
  }

}