using Centazio.Cli.Commands;
using Centazio.Cli.Utils;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli;

public class Cli(ICliSplash splash, ICommandTree commands, IInteractiveMenu menu, IServiceProvider svcs) {

  public int Start(string[] args) {
    splash.Show();
    var app = new CommandApp<InteractiveCliCommand>().WithData(menu);
    app.Configure(cfg => commands.Initialise(cfg, svcs));
    app.Run(args);
    return 0;
  }

  public void ReportException(Exception ex, bool terminate) {
    Log.Error(ex, $"unhandled exception");
    AnsiConsole.WriteException(ex,
        ExceptionFormats.ShortenPaths
        | ExceptionFormats.ShortenTypes
        | ExceptionFormats.ShortenMethods
        | ExceptionFormats.ShowLinks);
    if (terminate) Environment.Exit(-1);
  }

}