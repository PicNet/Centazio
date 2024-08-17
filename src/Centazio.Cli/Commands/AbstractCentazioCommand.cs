using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {

  string Id { get; }
  bool RunInteractiveCommand();

}

public interface ITreeCompatibleCentazioCommand : ICentazioCommand {
  void AddToBranch(IConfigurator<CommandSettings> branch);
}

public abstract class AbstractCentazioCommand<T, S>(string id) : Command<S>, ITreeCompatibleCentazioCommand
    where T : class, ICommandLimiter<CommandSettings>
    where S : CommandSettings {

  public void AddToBranch(IConfigurator<CommandSettings> branch) => branch.AddCommand<T>(id);
  
  public string Id => id;
  protected bool Interactive { get; private set; }

  public override int Execute(CommandContext context, S settings) {
    Interactive = false;
    ExecuteImpl(settings);
    return 0;
  }
  
  public bool RunInteractiveCommand() {
    Interactive = true;
    return RunInteractiveCommandImpl();
  }
  
  protected abstract bool RunInteractiveCommandImpl();
  protected abstract void ExecuteImpl(S settings);
  
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