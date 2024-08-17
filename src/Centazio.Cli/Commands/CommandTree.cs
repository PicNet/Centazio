using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommandTree {

  public IDictionary<string, List<string>> Root { get; } = new Dictionary<string, List<string>>();

  public void Initialise(IConfigurator cfg, IServiceProvider svcs) {
    AddBranch("aws", branch => {
      AddCommand<AccountsCommand>(branch, "accounts");
    });
    AddBranch("az", branch => {
      AddCommand<ResourceGroupsCommand>(branch, "rg");
    });
    // AddBranch("func", branch => { });
    // AddBranch("gen", branch => { });
    // AddBranch("dev", branch => { });
    
    void AddBranch(string name, Action<(string Name, IConfigurator<CommandSettings> Config)> action) {
      Root[name] = new List<string>(); 
      cfg.AddBranch(name, brcfg => action((name, brcfg)));
    }
    
    void AddCommand<T>((string Name, IConfigurator<CommandSettings> Config) branch, string id) where T : class, ICommandLimiter<CommandSettings> {
      Root[branch.Name].Add(id);
      branch.Config.AddCommand<T>(id);
    }
  }
  
}