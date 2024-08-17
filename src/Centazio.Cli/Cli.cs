using Centazio.Cli.Commands;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli;

public class Cli(CommandTree commands, InteractiveMenu menu, IServiceProvider svcs, ITypeRegistrar services) {

  public int Start(string[] args) {
    ShowSplash();
    
    var app = new CommandApp<InteractiveCliMeneCommand>(services)
        .WithData(menu);
    app.Configure(cfg => commands.Initialise(cfg, svcs));
    app.Run(args);
    
    return 0;
  }
  
  private void ShowSplash() {
    AnsiConsole.Write(new CanvasImage("swirl.png").MaxWidth(32));
    AnsiConsole.Write(new FigletText("Centazio").LeftJustified().Color(Color.Blue));
    AnsiConsole.MarkupLine("[link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
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