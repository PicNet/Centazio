using System.Diagnostics;
using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Commands.Host;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public abstract class Node(string id, Node? parent = null) {
  public Node? Parent { get; set; } = parent;
  public string Id => id;
}

public class BranchNode(string id, List<Node> children, string backlbl="back") : Node(id) {
  public string BackLabel => backlbl;
  public List<Node> Children => children;
}

public abstract class AbstractCommandNode(string id, ICentazioCommand cmd) : Node(id) {
  public abstract void AddTo(IConfigurator<CommandSettings> branch);
  public ICentazioCommand Command => cmd;
}

public class CommandNode<T>(string id, ICentazioCommand cmd) : AbstractCommandNode(id, cmd) where T : class, ICommandLimiter<CommandSettings> {
  public override void AddTo(IConfigurator<CommandSettings> branch) => branch.AddCommand<T>(Id);
}

public class CommandsTree {

  internal BranchNode RootNode { get; }
  private IServiceProvider Provider { get; }
  
  public CommandsTree(IServiceProvider prov) {
    Provider = prov;
    
    RootNode = new("centazio", [
      ///////////////////////////////////////////////
      // AWS
      ///////////////////////////////////////////////
      new BranchNode("aws", [
        new BranchNode("account", [
          CreateCommandNode<ListAccountsCommand>("list"),
          CreateCommandNode<AddAccountCommand>("add")
        ])
      ]),
      ///////////////////////////////////////////////
      // Azure
      ///////////////////////////////////////////////
      new BranchNode("az", [
        new BranchNode("sub", [
          CreateCommandNode<ListSubscriptionsCommand>("list")
        ]),
        new BranchNode("rg", [
          CreateCommandNode<ListResourceGroupsCommand>("list"),
          CreateCommandNode<AddResourceGroupCommand>("add"),
        ]),
        new BranchNode("func", [
          CreateCommandNode<GenerateAzFunctionsCommand>("generate"),
          CreateCommandNode<DeployAzFunctionsCommand>("deploy"),
          CreateCommandNode<DeleteAzFunctionsCommand>("delete"),
          CreateCommandNode<StartStopAzFunctionAppCommand>("start"),
          CreateCommandNode<StartStopAzFunctionAppCommand>("stop"),
          CreateCommandNode<AzFunctionLocalSimulateCommand>("simulate")
        ])
      ]),
      ///////////////////////////////////////////////
      // Local Host
      ///////////////////////////////////////////////
      new BranchNode("host", [
        CreateCommandNode<RunHostCommand>("run"),
      ]),
    ],
    "exit");
  }

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
        case AbstractCommandNode ln:
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

  private CommandNode<T> CreateCommandNode<T>(string id) where T : class, ICentazioCommand, ICommandLimiter<CommandSettings> => 
      new(id, Provider.GetRequiredService<T>());

}