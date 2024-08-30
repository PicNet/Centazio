using Spectre.Console;

namespace Centazio.Cli.Infra.Ui;

public static class UiHelpers {

  public static string Ask(string prompt, string? defaultval) => 
      string.IsNullOrWhiteSpace(defaultval) 
          ? AnsiConsole.Ask<string>(prompt + ":").Trim() 
          : AnsiConsole.Ask(prompt, defaultval.Trim()).Trim();

  public static string PromptCommandOptions(ICollection<string> options) => 
      AnsiConsole.Prompt(new SelectionPrompt<string>()
          .Title("Select Operation:")
          .AddChoices(options.Concat(new[] { "back" })));

  public static async Task Progress(string description, Func<Task> action) => 
      await AnsiConsole.Progress()
          .AutoClear(true)
          .Columns([new SpinnerColumn(), new TaskDescriptionColumn()])
          .StartAsync(async ctx => {
            ctx.AddTask($"[green]{description}[/]");
            await action();
          });

  public static async Task ProgressWithErrorMessage(string description, Func<Task<string>> action) {
    var error = await AnsiConsole.Progress()
        .AutoClear(true)
        .Columns([new SpinnerColumn(), new TaskDescriptionColumn()])
        .StartAsync(async ctx => {
          ctx.AddTask($"[green]{description}[/]");
          return await action();
        });
    if (!string.IsNullOrWhiteSpace(error)) AnsiConsole.WriteLine($"[red]{error}[/]");
  }

}