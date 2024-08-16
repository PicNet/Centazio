﻿using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class FallbackMenuCommand : AsyncCommand {
  public override Task<int> ExecuteAsync(CommandContext context) {
    var menu = context.Data as IInteractiveMenu ?? throw new Exception();
    return menu.ShowMenu(context);
  }
}

public interface IInteractiveMenu {
  Task<int> ShowMenu(CommandContext ctx);
}

public class InteractiveMenu(ICommandTree tree) : IInteractiveMenu {

  public Task<int> ShowMenu(CommandContext ctx) {
    var branch = ctx.Arguments.FirstOrDefault();
    if (String.IsNullOrWhiteSpace(branch) || !tree.Tree.ContainsKey(branch)) return ShowTopLevelMenu(String.IsNullOrWhiteSpace(branch) 
          ? "No command specified.  Please select one of the following supported options:" 
          : $"Top level command [{branch}] is not supported.  Please select one of the following supported options:", ctx);

    return ShowBranch(branch, ctx);
  }

  public async Task<int> ShowTopLevelMenu(string message, CommandContext ctx) {
    AnsiConsole.WriteLine();
    
    var branch = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title(message)
        .AddChoices(tree.Tree.Keys.Concat(new [] { "back" })));
    
    return String.IsNullOrWhiteSpace(branch) || branch == "back" 
        ? await ShowTopLevelMenu("Please select one of the following supported options:", new CommandContext(
            Array.Empty<string>(), 
            new EmptyRemainingArgs(), String.Empty, default)) 
        : await ShowBranch(branch, ctx);
  }

  public async Task<int> ShowBranch(string branch, CommandContext ctx) {
    var id = ctx.Arguments.Skip(1).FirstOrDefault();
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
    return await cmd.RunInteractiveCommand(ctx);
  }

  public class EmptyRemainingArgs : IRemainingArguments {
    
    public IReadOnlyList<string> Raw { get; } = Array.Empty<string>();
    
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
    public ILookup<string, string?> Parsed { get; } = Array.Empty<string>().ToLookup(_ => String.Empty);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

  }
}