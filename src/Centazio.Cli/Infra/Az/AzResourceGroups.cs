﻿using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;

namespace Centazio.Cli.Infra.Az;

public interface IAzResourceGroups {

  Task<IEnumerable<(string Id, string Name, string State, string ManagedBy)>> ListResourceGroups();
  Task<string> AddResourceGroup(string name);

}

public class AzResourceGroups(CliSecrets secrets) : AbstractAzCommunicator(secrets), IAzResourceGroups {

  public Task<IEnumerable<(string Id, string Name, string State, string ManagedBy)>> ListResourceGroups() {
      var subscription = GetClient().GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{Secrets.AZ_SUBSCRIPTION_ID}"));
      var rgs = subscription.GetResourceGroups().Select(rg => (rg.Id.Name, rg.Data.Name, rg.Data.ResourceGroupProvisioningState, rg.Data.ManagedBy ?? ""));
      return Task.FromResult(rgs);
    }
    
    public async Task<string> AddResourceGroup(string name) {
      var subscription = GetClient().GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{Secrets.AZ_SUBSCRIPTION_ID}"));
      var rgs = subscription.GetResourceGroups();
      var result = await rgs.CreateOrUpdateAsync(WaitUntil.Completed, name, new ResourceGroupData(AzureLocation.AustraliaEast));
      return result.HasCompleted ? "" : "Unknown failure";
    }

}