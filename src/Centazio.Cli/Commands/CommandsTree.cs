using System.Diagnostics;
using Centazio.Cli.Commands.Aws;
using Centazio.Cli.Commands.Az;
using Centazio.Cli.Commands.Dev;
using Centazio.Cli.Commands.Gen.Centazio;
using Centazio.Cli.Commands.Host;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public abstract class Node(string id, Node? parent = null) {
  public Node? Parent { get; set; } = parent;
  public string Id => id;
  public virtual bool IsValid => true;
}

public class BranchNode(string id, List<Node?> children, string backlbl="back") : Node(id) {

  public string BackLabel => backlbl;
  public List<Node> Children => children.OfType<Node>().Where(c => c.IsValid).ToList();
  
  // to be added to the cli, a branch must have at lease one valid child
  public override bool IsValid => Children.Any();
}

public abstract class AbstractCommandNode(string id, ICentazioCommand cmd) : Node(id) {
  public abstract void AddTo(IConfigurator<CommandSettings> branch);
  public ICentazioCommand Command => cmd;
}

public class CommandNode<T>(string id, ICentazioCommand cmd) : AbstractCommandNode(id, cmd) where T : class, ICommandLimiter<CommandSettings> {
  public override void AddTo(IConfigurator<CommandSettings> branch) => branch.AddCommand<T>(Id);
}

public class CommandsTree {
  
  private static readonly string EXIT_LABEL = "exit";
  
  internal BranchNode RootNode { get; }
  private IServiceProvider Provider { get; }
  
  public CommandsTree(IServiceProvider prov) {
    Provider = prov;
    
    var children = new List<Node?> {
      ///////////////////////////////////////////////
      // AWS
      ///////////////////////////////////////////////
      new BranchNode("aws", [
        new BranchNode("account", [
          CreateCommandNode<ListAccountsCommand>("list"),
          CreateCommandNode<AddAccountCommand>("add")
        ]),
        new BranchNode("func", [
          CreateCommandNode<GenerateAwsFunctionsCommand>("generate"),
          CreateCommandNode<DeployAwsLambdaFunctionCommand>("deploy"),
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
          CreateCommandNode<AzFunctionLocalSimulateCommand>("simulate"),
          CreateCommandNode<ShowAzFunctionLogStreamCommand>("logs")
        ])
      ]),
      ///////////////////////////////////////////////
      // Local Host
      ///////////////////////////////////////////////
      new BranchNode("host", [
        CreateCommandNode<RunHostCommand>("run"),
      ]),
      new BranchNode("gen", [
        CreateCommandNode<GenerateSlnCommand>("sln"),
        CreateCommandNode<GenerateFunctionCommand>("func")
      ]),
    };
    if (Env.IsInDev()) {
      children.Add(new BranchNode("dev", [
        CreateCommandNode<UiTestsCommand>("ui-test"),
        CreateCommandNode<GenerateSettingTypesCommand>("gen-settings"),
        CreateCommandNode<PackageAndPublishNuGetsCommand>("publish"),
      ]));
    }
    RootNode = new("centazio", children, EXIT_LABEL);
  }

  public void Initialise(IConfigurator cfg) {
    RootNode.Children
        .OfType<BranchNode>() 
        .ForEach(n => AddFirstLevelBranches(RootNode, n));
    
    void AddFirstLevelBranches(BranchNode root, BranchNode lvl1) {
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
  }

  public string GetNodeCommandShortcut(Node? node) {
    var ancestry = new List<Node>();
    while (node is not null) {
      ancestry.Insert(0, node);
      node = node.Parent;
    }
    return String.Join(' ', ancestry.Select(n2 => n2.Id));
  }

  private CommandNode<T>? CreateCommandNode<T>(string id) where T : class, ICentazioCommand, ICommandLimiter<CommandSettings> {
    // if the command is not registered then do not add it to the tree.  This is commonly only for commands
    //    that require CentazioSettings and they are not available.  See CliBootstrapper for code that
    //    does not register these commands if settings are not available.
    var command = Provider.GetService<T>();
    return command is null ? null : new CommandNode<T>(id, command);
  }

}