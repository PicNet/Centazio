﻿using Centazio.Cli.Commands.Aws;
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

public interface ICommandTree {
  IDictionary<string, List<ICentazioCommand>> Tree { get; }
  void Initialise(IConfigurator cfg, IServiceProvider svcs);
}

public class CommandTree : ICommandTree {

  public IDictionary<string, List<ICentazioCommand>> Tree { get; } = new Dictionary<string, List<ICentazioCommand>>();
  
  public void Initialise(IConfigurator cfg, IServiceProvider svcs) {
    AddBranch(cfg, "aws", [svcs.GetRequiredService<ResourceGroupCommand>()]);
    AddBranch(cfg, "az", []);
  }

  private void AddBranch(IConfigurator cfg, string name, List<ICentazioCommand> commands) {
    cfg.AddBranch(name, branch => {
      (Tree[name] = commands).ForEach(cmd => cmd.AddToBranch(branch));
      branch.SetDefaultCommand<FallbackMenuCommand>();
    });
  }

}