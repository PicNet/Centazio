using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AddResourceGroupCommand(CliSettings clisetts, IAzResourceGroups impl) : AbstractCentazioCommand<AddResourceGroupCommand.CreateResourceGroupCommandSettings> {
  
  protected override void RunInteractiveCommandImpl() => 
      _ = ExecuteImpl(new CreateResourceGroupCommandSettings { ResourceGroupName = UiHelpers.Ask("Resource Group Name", clisetts.DefaultResourceGroupName) });

  protected override async Task ExecuteImpl(CreateResourceGroupCommandSettings settings) {
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.ResourceGroupName);
    await UiHelpers.ProgressWithErrorMessage("Loading resource group list", async () => await impl.CreateResourceGroup(settings.ResourceGroupName));
  }

  public class CreateResourceGroupCommandSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string ResourceGroupName { get; init; } = null!;
  }
}