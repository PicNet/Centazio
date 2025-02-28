using Serilog.Events;
using Spectre.Console;

namespace Centazio.Cli.Infra.Ui;

public static class UiHelpers {

  public static void Log(string message, LogEventLevel level = LogEventLevel.Information) {
    var formatted = message.IndexOf("[/]", StringComparison.Ordinal) >= 0 
        ? message 
        : $"[{GetColour()}]{message.Trim()}[/]";  
    AnsiConsole.MarkupLine(formatted);
    
    string GetColour() => level switch {
      LogEventLevel.Fatal => "underline red",
      LogEventLevel.Error => "red",
      LogEventLevel.Warning => "orange3",
      LogEventLevel.Information => "yellow",
      LogEventLevel.Debug => "white",
      LogEventLevel.Verbose => "gray",
      _ => throw new NotSupportedException(level.ToString())
    };
  }
  
  public static string Ask(string prompt, string? defaultval = null) => 
      string.IsNullOrWhiteSpace(defaultval) 
          ? AnsiConsole.Ask<string>(prompt + ":").Trim() 
          : AnsiConsole.Ask(prompt, defaultval.Trim()).Trim();
  
  public static bool Confirm(string prompt, bool defaultval = false) =>
      AnsiConsole.Confirm(prompt + ":", defaultval);

  public static string Select(string selectlbl, List<string> options) => 
      AnsiConsole.Prompt(new SelectionPrompt<string>()
          .Title(selectlbl)
          .AddChoices(options));

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
    if (!string.IsNullOrWhiteSpace(error)) Log(error, LogEventLevel.Error);
  }

  public static void Table(List<string> headers, List<List<string>> rows) {
    var tbl = new Table().AddColumns(headers.ToArray());
    rows.ForEach(row => tbl.AddRow(row.ToArray()));
    AnsiConsole.Write(tbl);
  }
  
}