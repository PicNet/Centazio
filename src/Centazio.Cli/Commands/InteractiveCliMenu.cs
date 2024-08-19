using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class InteractiveCliMeneCommand : Command {
  public override int Execute(CommandContext context) {
    var menu = context.Data as InteractiveMenu ?? throw new Exception();
    menu.Show();
    return 0;
  }
}

public class InteractiveMenu(CommandTree tree) {

  public void Show() {
    while (DisplayNode(tree.RootNode)) { }
    AnsiConsole.MarkupLine("Thank you for using [link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public bool DisplayNode(Node n) {
    return n switch { 
      BranchNode bn => DisplayBranchNode(bn), 
      CommandNode cn => DisplayCommandNode(cn), 
      _ => throw new Exception() 
    };
  }

  private bool DisplayBranchNode(BranchNode n) {
    var branch = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Please select one of the following supported options:")
        .AddChoices(n.Children.Select(c => c.Id).Concat(new [] { n.BackLbl })));
    var selected = n.Children.Find(c => c.Id == branch);
    if (selected == null) return false;

    while (DisplayNode(selected)) {}
    return true;
  }
  
  private bool DisplayCommandNode(CommandNode n) {
    n.cmd.RunInteractiveCommand();
    AnsiConsole.WriteLine($"Command completed - {n.Id}");
    return false;
  }
}