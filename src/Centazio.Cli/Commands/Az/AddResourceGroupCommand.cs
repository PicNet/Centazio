using Centazio.Cli.Infra.Az;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AddResourceGroupCommand([FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSettings clisetts, IAzResourceGroups impl) : AbstractCentazioCommand<AddResourceGroupCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    ResourceGroupName = UiHelpers.Ask("Resource Group Name", clisetts.AzureSettings.ResourceGroup) 
  });

  public override async Task ExecuteImpl(Settings settings) {
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.ResourceGroupName);
    await UiHelpers.ProgressWithErrorMessage("Loading resource group list", async () => await impl.AddResourceGroup(settings.ResourceGroupName));
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public required string ResourceGroupName { get; init; }
  }
}