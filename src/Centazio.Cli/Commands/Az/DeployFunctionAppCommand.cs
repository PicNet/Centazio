using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Centazio.Core.Secrets;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployFunctionAppCommand(CentazioSecrets secrets) : AbstractCentazioCommand<DeployFunctionAppCommand.DeployFunctionAppCommandSettings> {
  
  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new DeployFunctionAppCommandSettings { 
        // ResourceGroupName = UiHelpers.Ask("Function Name", clisetts.AzureSettings?.Functions) 
      });

  protected override async Task ExecuteImpl(DeployFunctionAppCommandSettings settings) {
    // ArgumentException.ThrowIfNullOrWhiteSpace(settings.ResourceGroupName);
    // await UiHelpers.ProgressWithErrorMessage("Loading resource group list", async () => await impl.AddResourceGroup(settings.ResourceGroupName));
    
    var client = new ArmClient(new ClientSecretCredential(secrets.AZ_TENANT_ID, secrets.AZ_CLIENT_ID, secrets.AZ_SECRET_ID));
    var rg = (await client.GetResourceGroupResource(new ResourceIdentifier($"/subscriptions/your-subscription-id/resourceGroups/your-resource-group-name")).GetAsync()).Value;
    var app = (await rg.GetWebSiteAsync("your-function-app-name")).Value;
    var function = (await app.GetSiteFunctions().GetAsync("func-name")).Value;
    var op = (await function.UpdateAsync(WaitUntil.Completed, new FunctionEnvelopeData())).Value;
    if (op == null) throw new Exception($"");
    
    throw new Exception("implement");

  }

  public class DeployFunctionAppCommandSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string ResourceGroupName { get; init; } = null!;
  }
}