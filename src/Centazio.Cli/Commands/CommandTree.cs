using System.Diagnostics;
using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public abstract class Node(string id, Node? parent = null) {
  public Node? Parent { get; set; } = parent;
  public string Id => id;
}
public class BranchNode(string id, string backlbl, List<Node> children) : Node(id) {
  public string BackLabel => backlbl;
  public List<Node> Children => children;
}
public class CommandNode(string id, ICentazioCommand cmd, Action<IConfigurator<CommandSettings>> addto) : Node(id) {
  public ICentazioCommand Command => cmd;
  public Action<IConfigurator<CommandSettings>> AddTo => addto;
}

public class CommandTree(IServiceProvider prov) {
  
  public readonly BranchNode RootNode = new("centazio", "exit", [
    new BranchNode("aws", "back", [
      new BranchNode("account", "back", [
        new CommandNode("list", prov.GetRequiredService<ListAccountsCommand>(), branch => branch.AddCommand<ListAccountsCommand>("list")),
        new CommandNode("add", prov.GetRequiredService<AddAccountCommand>(), branch => branch.AddCommand<AddAccountCommand>("add"))
      ])
    ]),
    new BranchNode("az", "back", [
      new BranchNode("sub", "back", [
        new CommandNode("list", prov.GetRequiredService<ListSubscriptionsCommand>(), branch => branch.AddCommand<ListSubscriptionsCommand>("list"))
      ]),
      new BranchNode("rg", "back", [
        new CommandNode("list", prov.GetRequiredService<ListResourceGroupsCommand>(), branch => branch.AddCommand<ListResourceGroupsCommand>("list")),
        new CommandNode("add", prov.GetRequiredService<AddResourceGroupCommand>(), branch => branch.AddCommand<AddResourceGroupCommand>("list")),
      ]),
      /*new BranchNode("func", "back", [
        new CommandNode("create-app", prov.GetRequiredService<ListResourceGroupsCommand>(), branch => branch.AddCommand<ListResourceGroupsCommand>("list")),
        new CommandNode("create", prov.GetRequiredService<AddResourceGroupCommand>(), branch => branch.AddCommand<AddResourceGroupCommand>("list")),
        new CommandNode("deploy", prov.GetRequiredService<AddResourceGroupCommand>(), branch => branch.AddCommand<AddResourceGroupCommand>("list")),
      ]),*/
    ])
  ]);

  public void Initialise(IConfigurator cfg) {
    void AddChildToRootCfg(BranchNode root, BranchNode lvl1) {
      lvl1.Parent = root;
      cfg.AddBranch(lvl1.Id, branch => lvl1.Children.ForEach(
            lvl2 => AddNodeChildToParentCfg(branch, lvl1, lvl2)));
    }
    
    void AddNodeChildToParentCfg(IConfigurator<CommandSettings> parent, BranchNode parentnd, Node n) {
      n.Parent = parentnd;
      switch (n) {
        case BranchNode bn:
          parent.AddBranch(bn.Id, branch => bn.Children.ForEach(c => AddNodeChildToParentCfg(branch, bn, c)));
          break;
        case CommandNode ln:
          ln.AddTo(parent);
          break;
        default: throw new UnreachableException();
      }
    }
    
    RootNode.Children.ForEach(n => AddChildToRootCfg(RootNode, (BranchNode) n));
  }

  public string GetNodeCommandShortcut(Node? node) {
    var ancestry = new List<Node>();
    while (node is not null) {
      ancestry.Insert(0, node);
      node = node.Parent;
    }
    return String.Join(' ', ancestry.Select(n2 => n2.Id));
  }

}