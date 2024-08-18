using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public abstract record Node(string Id);
public record BranchNode(string Id, string BackLbl, List<Node> Children) : Node(Id);
public record CommandNode(string Id, ICentazioCommand cmd, Action<IConfigurator<CommandSettings>> addto) : Node(Id);

public class CommandTree(IServiceProvider prov) {

  public readonly BranchNode RootNode = new("root", "exit", [
    new BranchNode("aws", "back", [
      new BranchNode("account", "back", [
        new CommandNode("list", prov.GetRequiredService<AccountsCommand>(), branch => branch.AddCommand<ResourceGroupsCommand>("list")),
        new CommandNode("add", prov.GetRequiredService<AccountsCommand>(), branch => branch.AddCommand<ResourceGroupsCommand>("add"))
      ])
    ]),
    new BranchNode("az", "back", [
      new BranchNode("sub", "back", [
        new CommandNode("list", prov.GetRequiredService<ResourceGroupsCommand>(), branch => branch.AddCommand<ResourceGroupsCommand>("list"))
      ]),
      new BranchNode("rg", "back", [
        new CommandNode("list", prov.GetRequiredService<ResourceGroupsCommand>(), branch => branch.AddCommand<ResourceGroupsCommand>("list"))
      ]),
    ])
  ]);

  public void Initialise(IConfigurator cfg, IServiceProvider svcs) {
    void AddChildren(IConfigurator<CommandSettings> parent, Node n) {
      switch (n) {
        case BranchNode bn:
          parent.AddBranch(bn.Id, branch => bn.Children.ForEach(c => AddChildren(branch, c)));
          break;
        case CommandNode ln:
          ln.addto(parent);
          break;
        default: throw new Exception();
      }
    }
    RootNode.Children.ForEach(lvl1 => 
        cfg.AddBranch(lvl1.Id, branch => ((BranchNode)lvl1).Children.ForEach(
            lvl2 => AddChildren(branch, lvl2))));
  }
}