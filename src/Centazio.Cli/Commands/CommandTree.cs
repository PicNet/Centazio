using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public class CommandTree {

  public IDictionary<string, List<ICentazioCommand>> Root { get; } = new Dictionary<string, List<ICentazioCommand>>();

  public void Initialise(IConfigurator cfg, IServiceProvider svcs) {
    AddBranch("aws", [svcs.GetRequiredService<AccountsCommand>()]);
    AddBranch("az", [svcs.GetRequiredService<ResourceGroupsCommand>()]);
    AddBranch("func", [svcs.GetRequiredService<ResourceGroupsCommand>()]);
    AddBranch("gen", [svcs.GetRequiredService<ResourceGroupsCommand>()]);
    AddBranch("dev", [svcs.GetRequiredService<ResourceGroupsCommand>()]);
    
    void AddBranch(string name, List<ICentazioCommand> commands) => 
      cfg.AddBranch(name, branch => (Root[name] = commands).ForEach(cmd => cmd.AddToBranch(branch)));
  }
}