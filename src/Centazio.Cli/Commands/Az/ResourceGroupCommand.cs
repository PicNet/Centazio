using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class ResourceGroupCommand(CliSettings clisetts) : AbstractCentazioCommand<ResourceGroupCommand, ResourceGroupCommand.ResourceGroupSettings>("rg") {
  
  public override Task<int> RunInteractiveCommand(CommandContext ctx) {
    var op = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Select Operation:")
        .AddChoices(["List", "Create", "Delete"]));
    
    if (op == "List") return ExecuteAsync(ctx, new ResourceGroupSettings { List = true });
    return ExecuteAsync(ctx, new ResourceGroupSettings { 
      Create = op == "Create",
      Delete = op == "Delete",
      ResourceGroupName = AnsiConsole.Ask("Resource Group Name:", clisetts.DefaultResourceGroup)  
    });
  }

  public override Task<int> ExecuteAsync(CommandContext context, ResourceGroupSettings settings) {
    if (settings.List) return ListResourceGroups();
    if (settings.Create) return CreateResourceGroup(settings.ResourceGroupName);
    if (settings.Delete) return DeleteResourceGroup(settings.ResourceGroupName);
    throw new Exception($"Invalid settings state: " + JsonSerializer.Serialize(settings));
  }

  private Task<int> CreateResourceGroup(string name) {
    
    return Task.FromResult(0);
  }

  private static Task<int> DeleteResourceGroup(string name) {
    if (!AnsiConsole.Prompt(new ConfirmationPrompt($"Are you sure you want to delete the resource group [{name}] and all children resources?"))) {
      return Task.FromResult(0);
    }

    return Task.FromResult(0);
  }

  private Task<int> ListResourceGroups() { throw new NotImplementedException(); }

  public class ResourceGroupSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string ResourceGroupName { get; set; } = null!;
    
    [CommandOption("-l|--list")] public bool List { get; set; } = false;
    [CommandOption("-c|--create")] public bool Create { get; set; } = false;
    [CommandOption("-d|--delete")] public bool Delete { get; set; } = false;
  }

}