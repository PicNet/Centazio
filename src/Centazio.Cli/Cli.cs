using Centazio.Cli.Utils;
using Serilog;
using Spectre.Console;

namespace Centazio.Cli;

public class Cli(ICliSplash splash) {

  public void Start() {
    splash.Show();
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