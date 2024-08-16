using Centazio.Cli.Commands.Aws;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public abstract class AbstractCentazioCommand<T, S>(string id) : AsyncCommand<S>, ICentazioCommand
    where T : class, ICommandLimiter<CommandSettings>
    where S : CommandSettings {

  public string Id => id;
  public void AddToBranch(IConfigurator<CommandSettings> branch) => branch.AddCommand<T>(id);
  public abstract Task<int> RunInteractiveCommand(CommandContext ctx);

  protected async Task<int> Progress(string description, Func<Task<int>> action) => await AnsiConsole.Progress()
      .Columns([new SpinnerColumn(), new TaskDescriptionColumn()])
      .StartAsync(async ctx => {
        ctx.AddTask($"[green]{description}[/]");
        return await action();
      });

}

public interface ICentazioCommand {

  public string? Id { get; }
  public void AddToBranch(IConfigurator<CommandSettings> branch);
  Task<int> RunInteractiveCommand(CommandContext ctx);

}

public interface ICommandTree {

  IDictionary<string, List<ICentazioCommand>> Tree { get; }
  void Initialise(IConfigurator cfg, IServiceProvider svcs);

}

public class CommandTree : ICommandTree {

  public IDictionary<string, List<ICentazioCommand>> Tree { get; } = new Dictionary<string, List<ICentazioCommand>>();

  public void Initialise(IConfigurator cfg, IServiceProvider svcs) {
    AddBranch(cfg, "aws: AWS related commands", [svcs.GetRequiredService<AccountsCommand>()]);
    AddBranch(cfg, "az: Azure related commands", []);
    AddBranch(cfg, "func: Serverless function related commands", []);
    AddBranch(cfg, "gen: Code generators", []);
    AddBranch(cfg, "dev: Misc development commands", []);
  }

  private void AddBranch(IConfigurator cfg, string name, List<ICentazioCommand> commands) {
    cfg.AddBranch(name,
        branch => {
          (Tree[name] = commands).ForEach(cmd => cmd.AddToBranch(branch));
          branch.SetDefaultCommand<FallbackMenuCommand>();
        });
  }

}