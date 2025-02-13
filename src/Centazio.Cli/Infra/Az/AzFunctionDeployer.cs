using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using Azure;
using Azure.Core;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using net.r_eg.MvsSln.Extensions;
using Serilog;

namespace Centazio.Cli.Infra.Az;

public interface IAzFunctionDeployer {
  Task Deploy(FunctionProjectMeta project);
}

// try to replicate command: az functionapp deployment source config-zip -g <resource group name> -n <function app name> --src <zip file path>
public class AzFunctionDeployer(CentazioSettings settings, CentazioSecrets secrets) : AbstractAzCommunicator(secrets), IAzFunctionDeployer {

  
  public async Task Deploy(FunctionProjectMeta project) {
    if (!Directory.Exists(project.SolutionPath)) throw new Exception($"project [{project.ProjectName}] could not be found in the [{settings.GeneratedCodeFolder}] folder");
    if (!File.Exists(project.SlnFilePath)) throw new Exception($"project [{project}] does not appear to be a valid as no sln file was found");
    
    var rg = GetClient().GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(Secrets.AZ_SUBSCRIPTION_ID, settings.AzureSettings.ResourceGroup));
    var appres = await GetOrCreateFunctionApp(rg, project);

    await PublishFunctionApp(appres, project);
  }

  private async Task<WebSiteResource> GetOrCreateFunctionApp(ResourceGroupResource rg, FunctionProjectMeta project) {
    var appres = await GetFunctionAppIfExists(rg, project);
    if (appres is not null) return appres;

    Log.Information($"function app [{project}] does not exist, creating...");
    return await CreateNewFunctionApp(rg, project, settings.AzureSettings.Region);
  }

  private async Task<WebSiteResource?> GetFunctionAppIfExists(ResourceGroupResource rg, FunctionProjectMeta project) {
    try { return (await rg.GetWebSiteAsync(project.DashedProjectName)).Value; }
    catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
  }

  private async Task<WebSiteResource> CreateNewFunctionApp(ResourceGroupResource rg, FunctionProjectMeta project, string location) {
    var plandata = new AppServicePlanData(location) { Sku = new AppServiceSkuDescription { Name = "Y1", Tier = "Dynamic" }, Kind = "functionapp" };
    var appplan = (await rg.GetAppServicePlans().CreateOrUpdateAsync(WaitUntil.Completed, $"{project.DashedProjectName}-Plan", plandata)).Value;
    var appconf = CreateFunctionAppConfiguration(location, appplan.Id); 
    var op = await rg.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, project.DashedProjectName, appconf);
    Log.Information($"successfully created function app [{project}]");
    return op.Value;
  }

  private WebSiteData CreateFunctionAppConfiguration(string location, ResourceIdentifier farmid) => new(location) {
    Kind = "functionapp",
    SiteConfig = new SiteConfigProperties {
      NetFrameworkVersion = "v9.0",
      Use32BitWorkerProcess = false,
      AppSettings = [
        new AppServiceNameValuePair { Name = "FUNCTIONS_WORKER_RUNTIME", Value = "dotnet-isolated" },
        new AppServiceNameValuePair { Name = "FUNCTIONS_WORKER_RUNTIME_VERSION", Value = "9" },
        new AppServiceNameValuePair { Name = "FUNCTIONS_EXTENSION_VERSION", Value = "~4" },
        // todo: remove this hardcoded sb endpoint
        new AppServiceNameValuePair { Name = "AzureWebJobsStorage", Value = "Endpoint=sb://svcbusibyffnmkkoaf4.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=P7ukiUpfoai7rxWuRWckmEyTC49Rgn0GLdK3ULRIQuk=" },
        new AppServiceNameValuePair { Name = "SiteConfigProperties", Value = "1" },
        new AppServiceNameValuePair { Name = "WEBSITE_RUN_FROM_PACKAGE", Value = "1" }
      ]
    },
    AppServicePlanId = farmid
  };

  private async Task PublishFunctionApp(WebSiteResource appres, FunctionProjectMeta project) {
    var zippath = CreateFunctionAppZip(project);
    var cred = await GetPublishCredentials(appres);
    
    try { 
      var endpoint = $"https://{appres.Data.DefaultHostName.Replace("azurewebsites.net", "scm.azurewebsites.net")}/api/zipdeploy";

      var http = new HttpClient();
      var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cred.PublishingUserName}:{cred.PublishingPassword}"));
      http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

      var zipbytes = await File.ReadAllBytesAsync(zippath);
      using var content = new ByteArrayContent(zipbytes);
      content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

      Log.Information($"starting zip deployment: {zippath}");
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
  
  internal static string CreateFunctionAppZip(FunctionProjectMeta project) {
    var subdirs = new List<string> { ".azurefunctions" };
    var extensions = new List<string> { ".exe", ".dll", ".json", "*.metadata", ".pdb" };
    var zippath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
    
    using var zip = new FileStream(zippath, FileMode.Create);
    using var archive = new ZipArchive(zip, ZipArchiveMode.Create);
    
    // root directory, add files that match extension
    Directory.EnumerateFiles(project.PublishPath, "*.*", SearchOption.TopDirectoryOnly)
        .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
        .ForEach(file => archive.CreateEntryFromFile(file, Path.GetFileName(file)));
    
    // for each sub-directory add all files
    subdirs.ForEach(subdir => {
      var path = Path.Combine(project.PublishPath, subdir);
      if (!Directory.Exists(path)) return;
      Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
          .ForEach(file => archive.CreateEntryFromFile(file, Path.GetRelativePath(project.PublishPath, file)));
    });
    return zippath;
  }
}

