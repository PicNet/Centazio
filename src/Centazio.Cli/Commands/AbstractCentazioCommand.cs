using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {
  Task RunInteractiveCommand();
}

public abstract class AbstractCentazioCommand<S> : AsyncCommand<S>, ICentazioCommand where S : CommonSettings {

  protected bool Interactive { get; private set; }

  public override async Task<int> ExecuteAsync(CommandContext context, S settings) {
    Interactive = false;
    await ExecuteImpl(settings);
    return 0;
  }
  
  public async Task RunInteractiveCommand() {
    Interactive = true;
    var settings = await GetInteractiveSettings();
    await settings.SetInteractiveCommonOpts();
    await ExecuteImpl(settings);
  }

  protected abstract Task<S> GetInteractiveSettings();
  protected abstract Task ExecuteImpl(S settings);
}