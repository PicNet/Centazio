using System.Diagnostics;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class InteractiveCliMeneCommand : AsyncCommand {
  public override async Task<int> ExecuteAsync(CommandContext context) {
    var menu = context.Data as InteractiveMenu ?? throw new UnreachableException();
    await menu.Show();
    return 0;
  }
}

public class InteractiveMenu(CommandsTree tree) {

  public async Task Show() {
    while (await DisplayNode(tree.RootNode)) { }
    UiHelpers.Log("Thank you for using [link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public async Task<bool> DisplayNode(Node n) {
    return n switch { 
      BranchNode bn => await DisplayBranchNode(bn), 
      AbstractCommandNode cn => await DisplayCommandNode(cn), 
      _ => throw new UnreachableException() 
    };
  }

  private async Task<bool> DisplayBranchNode(BranchNode n) {
    var branch = UiHelpers.Select("Please select one of the following supported options:", n.Children.Select(c => c.Id).Concat([n.BackLabel]).ToList());
    var selected = n.Children.Find(c => c.Id == branch);
    if (selected is null) return false;

    while (await DisplayNode(selected)) {}
    return true;
  }
  
  private async Task<bool> DisplayCommandNode(AbstractCommandNode n) {
    await n.Command.RunInteractiveCommand(n.Id);
    
    UiHelpers.Log($"\nCommand completed: {tree.GetNodeCommandShortcut(n)} <options>\n");
    return false;
  }
}