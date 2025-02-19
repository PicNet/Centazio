using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {
  Task RunInteractiveCommand(string name);
}

public abstract class AbstractCentazioCommand<S> : AsyncCommand<S>, ICentazioCommand where S : CommonSettings {

  protected bool Interactive { get; private set; }

  public override async Task<int> ExecuteAsync(CommandContext context, S settings) {
    Interactive = false;
    await ExecuteImpl(context.Name, settings);
    return 0;
  }
  
  public async Task RunInteractiveCommand(string name) {
    Interactive = true;
    var settings = await GetInteractiveSettings();
    await settings.SetInteractiveCommonOpts();
    await ExecuteImpl(name, settings);
  }

  protected abstract Task<S> GetInteractiveSettings();
  protected abstract Task ExecuteImpl(string name, S settings);
}