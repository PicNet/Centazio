using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AddResourceGroupCommand(CliSettings clisetts, IAzResourceGroups impl) : AbstractCentazioCommand<AddResourceGroupCommand.CreateResourceGroupCommandSettings> {
  
  protected override bool RunInteractiveCommandImpl() {
    _ = ExecuteImpl(new CreateResourceGroupCommandSettings { ResourceGroupName = Ask("Resource Group Name", clisetts.DefaultResourceGroupName) });
    return true;
  }

  protected override async Task ExecuteImpl(CreateResourceGroupCommandSettings settings) {
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.ResourceGroupName);
    await ProgressWithErrorMessage("Loading resource group list", async () => await impl.CreateResourceGroup(settings.ResourceGroupName));
  }

  public class CreateResourceGroupCommandSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string ResourceGroupName { get; init; } = null!;
  }
}