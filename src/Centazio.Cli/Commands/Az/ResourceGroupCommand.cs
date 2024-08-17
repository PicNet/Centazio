using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class ResourceGroupsCommand(CliSettings clisetts, CliSecrets secrets) : AbstractCentazioCommand<ResourceGroupsCommand, ResourceGroupsCommand.ResourceGroupsCommandSettings>("rg") {
  
  private readonly ResourceGroupsCommandImpl impl = new(secrets);
  
  public override bool RunInteractiveCommand() {
    switch (PromptCommandOptions(["subscriptions", "list", "create"])) {
      case "back": return false;
      case "list": ExecuteImpl(new ResourceGroupsCommandSettings { List = true }); break;
      case "subscriptions": ExecuteImpl(new ResourceGroupsCommandSettings { ListSubscriptions = true }); break;
      case "create": ExecuteImpl(new ResourceGroupsCommandSettings { Create = true, ResourceGroupName = AnsiConsole.Ask("Resource Group Name:", clisetts.DefaultResourceGroupName) }); break;
      default: throw new Exception();
    }
    return true;
  }

  protected override void ExecuteImpl(ResourceGroupsCommandSettings settings) {
    if (settings.ListSubscriptions) ListSubscriptions();
    else if (settings.List) ListResourceGroups();
    else if (settings.Create) CreateResourceGroup(settings.ResourceGroupName);
    else throw new Exception($"Invalid settings state: " + JsonSerializer.Serialize(settings));
  }

  private async void ListSubscriptions() => 
      await Progress("Loading Subscriptions list", async () => 
          AnsiConsole.Write(new Table()
              .AddColumns(["Name", "Id", "State"])
              .AddRows((await impl.ListSubscriptions())
                  .Select(a => new [] { a.Name, a.Id, a.State }))));

  private async void ListResourceGroups() => 
      await Progress("Loading ResourceGroup list", async () => 
          AnsiConsole.Write(new Table()
              .AddColumns(["Name", "Id", "State", "ManagedBy"])
              .AddRows((await impl.ListResourceGroups())
                  .Select(a => new [] { a.Name, a.Id, a.State, a.ManagedBy }))));
  
  private void CreateResourceGroup(string name) => ProgressWithErrorMessage("Loading ResourceGroup list", async () => await impl.CreateResourceGroup(name));

  public class ResourceGroupsCommandSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string ResourceGroupName { get; init; } = null!;
    
    [CommandOption("-s|--subscriptions")] public bool ListSubscriptions { get; init; }
    [CommandOption("-l|--list")] public bool List { get; init; }
    [CommandOption("-c|--create")] public bool Create { get; init; }
  }
  
  public class ResourceGroupsCommandImpl(CliSecrets secrets) {
    
    public Task<IEnumerable<(string Id, string Name, string State)>> ListSubscriptions() {
      var subscriptions = GetClient().GetSubscriptions();
      return Task.FromResult(subscriptions.Select(s => (s.Id.Name, s.Data.DisplayName, s.Data.State?.ToString() ?? "")));
    }
    
    public Task<IEnumerable<(string Id, string Name, string State, string ManagedBy)>> ListResourceGroups() {
      var subscription = GetClient().GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{secrets.AZ_SUBSCRIPTION_ID}"));
      var rgs = subscription.GetResourceGroups().Select(rg => (rg.Id.Name, rg.Data.Name, rg.Data.ResourceGroupProvisioningState, rg.Data.ManagedBy ?? ""));
      return Task.FromResult(rgs);
    }
    
    public async Task<string> CreateResourceGroup(string name) {
      var subscription = GetClient().GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{secrets.AZ_SUBSCRIPTION_ID}"));
      var rgs = subscription.GetResourceGroups();
      var result = await rgs.CreateOrUpdateAsync(WaitUntil.Completed, name, new ResourceGroupData(AzureLocation.AustraliaEast));
      return result.HasCompleted ? "" : "Unknown failure";
    }

    private ArmClient GetClient() => new(new ClientSecretCredential(secrets.AZ_TENANT_ID, secrets.AZ_CLIENT_ID, secrets.AZ_SECRET_ID));

  }
}