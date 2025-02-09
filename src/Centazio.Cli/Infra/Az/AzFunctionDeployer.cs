using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using Azure;
using Azure.Core;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Serilog;

namespace Centazio.Cli.Infra.Az;

public interface IAzFunctionDeployer {
  Task Deploy(string project);
}

// todo: add unit test to test both 'new' and 'update' cases
public class AzFunctionDeployer(CentazioSettings settings, CentazioSecrets secrets) : AbstractAzCommunicator(secrets), IAzFunctionDeployer {

  
  public async Task Deploy(string project) {
    if (!Directory.Exists(FsUtils.GetSolutionFilePath(settings.GeneratedCodeFolder, project))) throw new Exception($"project [{project}] could not be found in the [{settings.GeneratedCodeFolder}] folder");
    if (!File.Exists(FsUtils.GetSolutionFilePath(settings.GeneratedCodeFolder, project, project + ".sln"))) throw new Exception($"project [{project}] does not appear to be a valid project directory as no sln file was found");
    
    var rg = GetClient().GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(Secrets.AZ_SUBSCRIPTION_ID, settings.AzureSettings.ResourceGroup));
    var appres = await GetOrCreateFunctionApp(rg, project);

    // todo: do we need this update?  Not just deploy?
    // Log.Information($"updating already existing function app [{project}]");
    // await UpdateExistingFunctionApp(appres, settings.AzureSettings.Region);

    await PublishFunctionApp(appres, project);
  }

  private async Task<WebSiteResource> GetOrCreateFunctionApp(ResourceGroupResource rg, string project) {
    var appres = await GetFunctionAppIfExists(rg, project);
    if (appres is not null) return appres;

    Log.Information($"function app [{project}] does not exist, creating...");
    return await CreateNewFunctionApp(rg, project, settings.AzureSettings.Region);
  }

  private async Task<WebSiteResource?> GetFunctionAppIfExists(ResourceGroupResource rg, string project) {
    try { return (await rg.GetWebSiteAsync(project)).Value; }
    catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
  }

  private async Task<WebSiteResource> CreateNewFunctionApp(ResourceGroupResource rg, string project, string location) {
    var plandata = new AppServicePlanData(location) { Sku = new AppServiceSkuDescription { Name = "Y1", Tier = "Dynamic" }, Kind = "functionapp" };
    var appplan = (await rg.GetAppServicePlans().CreateOrUpdateAsync(WaitUntil.Completed, $"{project}-plan", plandata)).Value;
    var appconf = CreateFunctionAppConfiguration(location, appplan.Id); 
    var op = await rg.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, project, appconf);
    Log.Information($"successfully created function app [{project}]");
    return op.Value;
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
    await funcapp.UpdateAsync(new SitePatchInfo { SiteConfig = updatedConfig });
    Log.Information($"updated function app [{funcapp.Data.Name}]");
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

  private async Task PublishFunctionApp(WebSiteResource appres, string project) {
    var projpath = FsUtils.GetSolutionFilePath(settings.GeneratedCodeFolder, project);
    var path = await new MicrosoftBuildProjectBuilder().BuildProject(projpath);
    var zippath = CreateFunctionAppZip(path);
    var cred = await GetPublishCredentials(appres);
    
    try { 
      var endpoint = $"https://{appres.Data.DefaultHostName.Replace("azurewebsites.net", "scm.azurewebsites.net")}/api/zipdeploy";

      var http = new HttpClient();
      var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cred.PublishingUserName}:{cred.PublishingPassword}"));
      http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

      var zipbytes = await File.ReadAllBytesAsync(zippath);
      using var content = new ByteArrayContent(zipbytes);
      content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

      Log.Information("starting zip deployment");
      var response = await http.PostAsync(endpoint, content);

      if (!response.IsSuccessStatusCode) {
        var err = await response.Content.ReadAsStringAsync();
        throw new Exception($"deployment failed with status [{response.StatusCode}], error [{err}]");
      }

      Log.Information("restarting function app");
      await appres.RestartAsync();

      Log.Information("deployment completed");
    }
    finally {
      if (File.Exists(zippath)) File.Delete(zippath);
    }
  }

  private async Task<PublishingUserData> GetPublishCredentials(WebSiteResource appres) => 
      (await appres.GetPublishingCredentialsAsync(WaitUntil.Completed)).Value.Data;
  

  private string CreateFunctionAppZip(string publishpath) {
    var zippath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
    ZipFile.CreateFromDirectory(Path.Combine(FsUtils.GetSolutionFilePath(publishpath)), zippath);
    return zippath;
  }

}