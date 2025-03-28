﻿using System.Net.Http.Headers;
using System.Text;
using Azure;
using Azure.Core;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Cli.Infra.Az;

public interface IAzFunctionDeployer {
  Task Deploy(AzureFunctionProjectMeta project);
}

// try to replicate command: az functionapp deployment source config-zip -g <resource group name> -n <function app name> --src <zip file path>
public class AzFunctionDeployer(CentazioSettings settings, CentazioSecrets secrets) : AbstractAzCommunicator(secrets), IAzFunctionDeployer {

  
  public async Task Deploy(AzureFunctionProjectMeta project) {
    if (!Directory.Exists(project.SolutionDirPath)) throw new Exception($"project [{project.ProjectName}] could not be found in the [{settings.Defaults.GeneratedCodeFolder}] folder");
    if (!File.Exists(project.SlnFilePath)) throw new Exception($"project [{project.ProjectName}] does not appear to be a valid as no sln file was found");
    
    var rg = GetClient().GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(Secrets.AZ_SUBSCRIPTION_ID, settings.AzureSettings.ResourceGroup));
    var appres = await GetOrCreateFunctionApp(rg, project);

    await PublishFunctionApp(appres, project);
  }

  private async Task<WebSiteResource> GetOrCreateFunctionApp(ResourceGroupResource rg, AzureFunctionProjectMeta project) {
    var appres = await GetFunctionAppIfExists(rg, project);
    if (appres is not null) return appres;

    return await CreateNewFunctionApp(rg, project, settings.AzureSettings.Region);
  }

  private async Task<WebSiteResource?> GetFunctionAppIfExists(ResourceGroupResource rg, AzureFunctionProjectMeta project) {
    try { return (await rg.GetWebSiteAsync(project.DashedProjectName)).Value; }
    catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
  }

  private async Task<WebSiteResource> CreateNewFunctionApp(ResourceGroupResource rg, AzureFunctionProjectMeta project, string location) {
    var plandata = new AppServicePlanData(location) { Sku = new AppServiceSkuDescription { Name = "Y1", Tier = "Dynamic" }, Kind = "functionapp" };
    var name = settings.AzureSettings.AppServicePlan ?? $"{project.DashedProjectName}-Plan";
    var appplan = (await rg.GetAppServicePlans().CreateOrUpdateAsync(WaitUntil.Completed, name, plandata)).Value;
    var appconf = CreateFunctionAppConfiguration(location, appplan.Id); 
    var op = await rg.GetWebSites().CreateOrUpdateAsync(WaitUntil.Completed, project.DashedProjectName, appconf);
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
        new AppServiceNameValuePair { Name = "WEBSITE_RUN_FROM_PACKAGE", Value = "1" },
        new AppServiceNameValuePair { Name = "SiteConfigProperties", Value = "1" },
        new AppServiceNameValuePair { Name = "AzureWebJobsStorage", Value = Secrets.AZ_BLOB_STORAGE_ENDPOINT },
        new AppServiceNameValuePair { Name = "APPLICATIONINSIGHTS_CONNECTION_STRING", Value = Secrets.AZ_APP_INSIGHT_CONNECTION_STRING },
      ]
    },
    AppServicePlanId = farmid
  };

  private async Task PublishFunctionApp(WebSiteResource appres, AzureFunctionProjectMeta project) {
    var zipbytes = await Zip.ZipDir(project.PublishPath, [".exe", ".dll", ".json", ".env", "*.metadata", ".pdb"], [".azurefunctions", "runtimes"]);
    var cred = await GetPublishCredentials(appres);
    
    var endpoint = $"https://{appres.Data.DefaultHostName.Replace("azurewebsites.net", "scm.azurewebsites.net")}/api/zipdeploy";

    var http = new HttpClient();
    var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cred.PublishingUserName}:{cred.PublishingPassword}"));
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
    
    using var content = new ByteArrayContent(zipbytes);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

    var response = await http.PostAsync(endpoint, content);

    if (!response.IsSuccessStatusCode) {
      var err = await response.Content.ReadAsStringAsync();
      throw new Exception($"deployment failed with status [{response.StatusCode}], error [{err}]");
    }

    await appres.RestartAsync();
  }

  private async Task<PublishingUserData> GetPublishCredentials(WebSiteResource appres) => 
      (await appres.GetPublishingCredentialsAsync(WaitUntil.Completed)).Value.Data;
  
}

