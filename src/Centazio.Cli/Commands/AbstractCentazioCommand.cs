using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {
  bool RunInteractiveCommand();
}

public abstract class AbstractCentazioCommand<S> : AsyncCommand<S>, ICentazioCommand where S : CommandSettings {

  protected bool Interactive { get; private set; }

  public override async Task<int> ExecuteAsync(CommandContext context, S settings) {
    Interactive = false;
    await ExecuteImpl(settings);
    return 0;
  }
  
  public bool RunInteractiveCommand() {
    Interactive = true;
    return RunInteractiveCommandImpl();
  }
  
  protected abstract bool RunInteractiveCommandImpl();
  protected abstract Task ExecuteImpl(S settings);
  
  protected string Ask(string prompt, string defaultval) {
    return String.IsNullOrWhiteSpace(defaultval) 
        ? AnsiConsole.Ask<string>(prompt + ":").Trim()
        : AnsiConsole.Ask(prompt, defaultval.Trim()).Trim();
  }

  protected string PromptCommandOptions(ICollection<string> options) {
    return AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Select Operation:")
        .AddChoices(options.Concat(new [] {"back"})));
  }
  
  protected async Task Progress(string description, Func<Task> action) => await AnsiConsole.Progress()
      .Columns([new SpinnerColumn(), new TaskDescriptionColumn()])
      .StartAsync(async ctx => {
        ctx.AddTask($"[green]{description}[/]");
        await action();
      });
  
  protected async Task ProgressWithErrorMessage(string description, Func<Task<string>> action) {
    var error = await AnsiConsole.Progress()
        .Columns([new SpinnerColumn(), new TaskDescriptionColumn()])
        .StartAsync(async ctx => {
          ctx.AddTask($"[green]{description}[/]");
          return await action();
        });
    if (!String.IsNullOrWhiteSpace(error)) AnsiConsole.WriteLine($"[red]{error}[/]");
  }

}