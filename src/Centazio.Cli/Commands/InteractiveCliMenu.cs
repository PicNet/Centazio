using System.Diagnostics;
using Spectre.Console;
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
    AnsiConsole.MarkupLine("Thank you for using [link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public async Task<bool> DisplayNode(Node n) {
    return n switch { 
      BranchNode bn => await DisplayBranchNode(bn), 
      AbstractCommandNode cn => await DisplayCommandNode(cn), 
      _ => throw new UnreachableException() 
    };
  }

  private async Task<bool> DisplayBranchNode(BranchNode n) {
    var branch = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Please select one of the following supported options:")
        .AddChoices(n.Children.Select(c => c.Id).Concat([n.BackLabel])));
    var selected = n.Children.Find(c => c.Id == branch);
    if (selected is null) return false;

    while (await DisplayNode(selected)) {}
    return true;
  }
  
  private async Task<bool> DisplayCommandNode(AbstractCommandNode n) {
    await n.Command.RunInteractiveCommand(n.Id);
    
    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine($"Command completed: {tree.GetNodeCommandShortcut(n)} [opts]");
    AnsiConsole.WriteLine();
    return false;
  }
}