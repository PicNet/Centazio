using Centazio.Cli.Commands;
using Centazio.Cli.Infra.Ui;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli;

public class Cli(CommandsTree commands, InteractiveMenu menu, ITypeRegistrar services) {

  public int Start(string[] args) {
    ShowSplash();
    
    var app = new CommandApp<InteractiveCliMenuCommand>(services)
        .WithData(menu);
    app.Configure(cfg => {
#if DEBUG
      cfg.PropagateExceptions();
      cfg.ValidateExamples();
#endif
      commands.Initialise(cfg);
    });
    try { return app.Run(args); }
    catch (Exception e) { 
      if (e is CommandParseException or CommandRuntimeException) { UiHelpers.Log(e.Message, LogEventLevel.Error); }
      else throw;
    }
    
    return -1;
  }
  
  private void ShowSplash() {
    UiHelpers.Log("\n\n");
    AnsiConsole.Write(new CanvasImage(FsUtils.GetCliDir("swirl.png")).MaxWidth(32));
    AnsiConsole.Write(new FigletText("Centazio").LeftJustified().Color(Color.Blue));
    UiHelpers.Log("[link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public void ReportException(Exception ex, bool terminate) {
    Log.Error(ex, $"unhandled exception");
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShowLinks);
    if (terminate) Environment.Exit(-1);
  }

}