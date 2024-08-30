﻿using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AddResourceGroupCommand(CentazioSettings clisetts, IAzResourceGroups impl) : AbstractCentazioCommand<AddResourceGroupCommand.AddResourceGroupCommandSettings> {
  
  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new AddResourceGroupCommandSettings { ResourceGroupName = UiHelpers.Ask("Resource Group Name", clisetts.AzureSettings?.ResourceGroup) });

  protected override async Task ExecuteImpl(AddResourceGroupCommandSettings settings) {
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.ResourceGroupName);
    await UiHelpers.ProgressWithErrorMessage("Loading resource group list", async () => await impl.AddResourceGroup(settings.ResourceGroupName));
  }

  public class AddResourceGroupCommandSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string ResourceGroupName { get; init; } = null!;
  }
}