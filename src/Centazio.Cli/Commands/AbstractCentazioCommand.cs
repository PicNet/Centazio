using Spectre.Console.Cli;

namespace Centazio.Cli.Commands;

public interface ICentazioCommand {
  void RunInteractiveCommand();
}

public abstract class AbstractCentazioCommand<S> : AsyncCommand<S>, ICentazioCommand where S : CommandSettings {

  protected bool Interactive { get; private set; }

  public override async Task<int> ExecuteAsync(CommandContext context, S settings) {
    Interactive = false;
    await ExecuteImpl(settings);
    return 0;
  }
  
  public void RunInteractiveCommand() {
    Interactive = true;
    RunInteractiveCommandImpl();
  }
  
  protected abstract void RunInteractiveCommandImpl();
  protected abstract Task ExecuteImpl(S settings);
}