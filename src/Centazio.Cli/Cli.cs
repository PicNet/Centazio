using Centazio.Cli.Commands;
using Centazio.Cli.Utils;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli;

public class Cli(ICliSplash splash) {

  public async Task<int> Start(string[] args) {
    splash.Show();
    var app = new CommandApp<FallbackMenuCommand>();
    app.Configure(CommandTree.Initialise);
    return await app.RunAsync(args);
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