﻿using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {
  Task RunInteractiveCommand(string name);
}

public abstract class AbstractCentazioCommand<S> : AsyncCommand<S>, ICentazioCommand where S : CommonSettings {

  protected string CommandName { get; private set; } = null!;
  protected bool Interactive { get; private set; }

  public override async Task<int> ExecuteAsync(CommandContext context, S settings) {
    (Interactive, CommandName) = (false, context.Name);
    await ExecuteImpl(settings);
    return 0;
  }
  
  public async Task RunInteractiveCommand(string name) {
    (Interactive, CommandName) = (true, name);
    var settings = await GetInteractiveSettings();
    await settings.SetInteractiveCommonOpts();
    await ExecuteImpl(settings);
  }

  protected abstract Task<S> GetInteractiveSettings();
  protected abstract Task ExecuteImpl(S settings);
}