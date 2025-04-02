using System.Diagnostics;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class InteractiveCliMenuCommand : AsyncCommand {
  public override async Task<int> ExecuteAsync(CommandContext context) {
    var menu = context.Data as InteractiveMenu ?? throw new UnreachableException();
    await menu.Show();
    return 0;
  }
}

public class InteractiveMenu(CommandsTree tree) {

  public async Task Show() {
    await DisplayNode(tree.RootNode);
    UiHelpers.Log("\nThank you for using [link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public async Task DisplayNode(Node? n) {
    if (n is null) return;
    await DisplayNode(n switch { 
      BranchNode bn => DisplayBranchNode(bn), 
      AbstractCommandNode cn => await DisplayCommandNode(cn), 
      _ => throw new UnreachableException() 
    });
  }

  private Node? DisplayBranchNode(BranchNode n) {
    var branch = UiHelpers.Select("Please select one of the following supported options:", n.Children.Select(c => c.Id).Concat([n.BackLabel]).ToList());
    return branch == n.BackLabel ? n.Parent : n.Children.Single(c => c.Id == branch);
  }
  
  private async Task<Node?> DisplayCommandNode(AbstractCommandNode n) {
    await n.Command.RunInteractiveCommand(n.Id);
    
    UiHelpers.Log($"\nCommand '{tree.GetNodeCommandShortcut(n)}' completed.");
    return null;
  }
}