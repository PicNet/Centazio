using Centazio.Cli.Commands.Aws;
using Centazio.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public abstract class AbstractCentazioCommand<T, S>(string id) : AsyncCommand<S>, ICentazioCommand 
      where T : class, ICommandLimiter<CommandSettings> 
      where S : CommandSettings {

  public string Id => id;
  public void AddToBranch(IConfigurator<CommandSettings> branch) => branch.AddCommand<T>(id);
  public abstract Task<int> RunInteractiveCommand();

}

public interface ICentazioCommand {
  public string? Id { get; }
  public void AddToBranch(IConfigurator<CommandSettings> branch);
  Task<int> RunInteractiveCommand();

}

public static class CommandTree {

  internal static readonly IDictionary<string, List<ICentazioCommand>> Tree = new Dictionary<string, List<ICentazioCommand>> {
    {"aws", [new ResourceGroupCommand()] },
    {"az", [new ResourceGroupCommand()] },
  };
  
  public static void Initialise(IConfigurator cfg, ServiceCollection svcs) {
    
    Tree.Keys.ForEachIdx(branch => cfg.AddBranch(branch, b => {
      Tree[branch].ForEachIdx(cmd => cmd.AddToBranch(b));
      b.SetDefaultCommand<FallbackMenuCommand>();
    }));
  }

}