using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {

  public string Id { get; }
  public void AddToBranch(IConfigurator<CommandSettings> branch);
  bool RunInteractiveCommand();

}

public abstract class AbstractCentazioCommand<T, S>(string id) : Command<S>, ICentazioCommand
    where T : class, ICommandLimiter<CommandSettings>
    where S : CommandSettings {
  
  public string Id => id;
  public void AddToBranch(IConfigurator<CommandSettings> branch) => branch.AddCommand<T>(id);
  public abstract bool RunInteractiveCommand();

  public override int Execute(CommandContext context, S settings) {
    ExecuteImpl(settings);
    return 0;
  }
  
  protected abstract void ExecuteImpl(S settings);

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
  
  protected async void ProgressWithErrorMessage(string description, Func<Task<string>> action) {
    var error = await AnsiConsole.Progress()
        .Columns([new SpinnerColumn(), new TaskDescriptionColumn()])
        .StartAsync(async ctx => {
          ctx.AddTask($"[green]{description}[/]");
          return await action();
        });
    if (!String.IsNullOrWhiteSpace(error)) AnsiConsole.WriteLine($"[red]{error}[/]");
  }

}