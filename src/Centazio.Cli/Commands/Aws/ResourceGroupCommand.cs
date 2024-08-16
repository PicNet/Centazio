using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class ResourceGroupCommand() : AbstractCentazioCommand<ResourceGroupCommand, ResourceGroupCommand.ResourceGroupSettings>("rg") {

  public override Task<int> ExecuteAsync(CommandContext context, ResourceGroupSettings settings) {
    return Task.FromResult(0);
  }

  public class ResourceGroupSettings : CommonSettings {
    [CommandArgument(0, "<RESOURCE_GROUP_NAME>")] public string PackageName { get; set; } = null!;
    
    [CommandOption("-l|--list")] public bool List { get; set; } = false;
    [CommandOption("-c|--create")] public bool Create { get; set; } = false;
    [CommandOption("-d|--delete")] public bool Delete { get; set; } = false;
  }

  public override Task<int> RunInteractiveCommand() {
    throw new NotImplementedException();
  }
}