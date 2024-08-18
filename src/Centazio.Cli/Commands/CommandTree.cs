using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommandTree {

  public IDictionary<string, List<string>> Root { get; } = new Dictionary<string, List<string>>();

  public void Initialise(IConfigurator cfg, IServiceProvider svcs) {
    AddBranch(cfg, "aws", branch => {
      AddCommand<AccountsCommand>(branch, "accounts");
      AddBranch(branch.Config, "aws2", branch2 => {
        AddCommand<AccountsCommand>(branch2, "accounts2");
      });
    });
    AddBranch(cfg, "az", branch => {
      AddCommand<ResourceGroupsCommand>(branch, "rg");
    });
    // AddBranch("func", branch => { });
    // AddBranch("gen", branch => { });
    // AddBranch("dev", branch => { });
  }
  
  private void AddBranch(IConfigurator cfg, string name, Action<(string Name, IConfigurator<CommandSettings> Config)> action) {
    Root[name] = new List<string>(); 
    cfg.AddBranch(name, brcfg => action((name, brcfg)));
  }
  
  private void AddBranch(IConfigurator<CommandSettings> cfg, string name, Action<(string Name, IConfigurator<CommandSettings> Config)> action) {
    Root[name] = new List<string>(); 
    cfg.AddBranch(name, brcfg => action((name, brcfg)));
  }
  
  private void AddCommand<T>((string Name, IConfigurator<CommandSettings> Config) branch, string id) where T : class, ICommandLimiter<CommandSettings> {
    Root[branch.Name].Add(id);
    branch.Config.AddCommand<T>(id);
  }
  
}