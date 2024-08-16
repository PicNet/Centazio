using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class FallbackMenuCommand : AsyncCommand {
  public override Task<int> ExecuteAsync(CommandContext context) {
    var menu = context.Data as IInteractiveMenu ?? throw new Exception();
    return menu.ShowMenu(context.Arguments);
  }
}

public interface IInteractiveMenu {
  Task<int> ShowMenu(IReadOnlyList<string> contextArguments);
}

public class InteractiveMenu(ICommandTree tree) : IInteractiveMenu {

  public Task<int> ShowMenu(IReadOnlyList<string> args) {
    var branch = args.FirstOrDefault();
    if (String.IsNullOrWhiteSpace(branch) || !tree.Tree.ContainsKey(branch)) return ShowTopLevelMenu(String.IsNullOrWhiteSpace(branch) 
          ? "No command specified.  Please select one of the following supported options:" 
          : $"Top level command [{branch}] is not supported.  Please select one of the following supported options:");

    return ShowBranch(branch, args.Skip(1).ToList());
  }

  public async Task<int> ShowTopLevelMenu(string message) {
    AnsiConsole.WriteLine();
    var branch = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title(message)
        .AddChoices(tree.Tree.Keys.Concat(new [] { "exit" })));
    return String.IsNullOrWhiteSpace(branch) || branch == "exit" ? 0 : await ShowBranch(branch, []);
  }

  public async Task<int> ShowBranch(string branch, List<string> args) {
    var id = args.FirstOrDefault();
    var cmds = tree.Tree[branch];
    var cmd = cmds.Find(c => c.Id == id);
    if (cmd == null) {
      var msg = String.IsNullOrWhiteSpace(id) 
            ? $"{branch} command not specified.  Please select one of the following supported commands:" 
            : $"{branch} command [{id}] is not supported.  Please select one of the following supported commands:";
      var cmdids = cmds.Select(c => c.Id).Concat(new [] { "exit" }).ToList();
      var newid = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title(msg)
        .AddChoices(cmdids));
      if (String.IsNullOrWhiteSpace(newid) || newid == "exit") return 0;
      cmd = cmds.Find(c => c.Id == newid) 
          ?? throw new Exception($"Error finding command [{newid}] in branch [{branch}]. Available commands in branch are [{String.Join(',', cmdids)}]");
    }
    return await cmd.RunInteractiveCommand();
  }

}