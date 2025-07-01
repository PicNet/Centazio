using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;

namespace Centazio.Cli.Infra.Az;

public interface IAzResourceGroups {

  Task<List<(string Id, string Name, string State, string ManagedBy)>> ListResourceGroups();
  Task<string> AddResourceGroup(string name);

}

public class AzResourceGroups(ICliSecretsManager loader) : AbstractAzCommunicator(loader), IAzResourceGroups {

  public async Task<List<(string Id, string Name, string State, string ManagedBy)>> ListResourceGroups() {
    var (secrets, client) = (await GetSecrets(), await GetClient());
    var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{secrets.AZ_SUBSCRIPTION_ID}"));
    return subscription.GetResourceGroups().Select(rg => (rg.Id.Name, rg.Data.Name, rg.Data.ResourceGroupProvisioningState, rg.Data.ManagedBy ?? String.Empty)).ToList();
  }
    
  public async Task<string> AddResourceGroup(string name) {
    var (secrets, client) = (await GetSecrets(), await GetClient());
    var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{secrets.AZ_SUBSCRIPTION_ID}"));
    var rgs = subscription.GetResourceGroups();
    var result = await rgs.CreateOrUpdateAsync(WaitUntil.Completed, name, new ResourceGroupData(AzureLocation.AustraliaEast));
    return result.HasCompleted ? String.Empty : "Unknown failure";
  }

}