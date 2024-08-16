﻿using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class InteractiveCliCommand : Command {
  public override int Execute(CommandContext context) {
    var menu = context.Data as IInteractiveMenu ?? throw new Exception();
    menu.Show();
    return 0;
  }
}

public interface IInteractiveMenu { void Show(); }

public class InteractiveMenu(ICommandTree tree) : IInteractiveMenu {

  public void Show() {
    while (ShowTopLevelMenu()) { }
    AnsiConsole.MarkupLine("Thank you for using [link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public bool ShowTopLevelMenu() {
    var branch = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Please select one of the following supported options:")
        .AddChoices(tree.Tree.Keys.Concat(new [] { "exit" })));
    if (branch == "exit") return false;
    while (ShowBranch(branch)) {}
    return true;
  }

  public bool ShowBranch(string branch) {
    var cmdid = SelectCommandId();
    return cmdid != "back" && RunCommand();
  
    string SelectCommandId() {
      var cmdids = tree.Tree[branch].Select(c => c.Id).Concat(new [] { "back" }).ToList();
      return AnsiConsole.Prompt(new SelectionPrompt<string>()
          .Title("Please select one of the following supported commands:")
          .AddChoices(cmdids));
    }

    bool RunCommand() {
      var cmd = tree.Tree[branch].Find(c => c.Id == cmdid) ?? throw new Exception();
      while (cmd.RunInteractiveCommand()) {}
      return true;
    }
  }
}