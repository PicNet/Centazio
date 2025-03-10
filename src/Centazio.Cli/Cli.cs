using Centazio.Cli.Commands;
using Centazio.Cli.Infra.Ui;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli;

public class Cli(CommandsTree commands, InteractiveMenu menu, ITypeRegistrar services) {

  public int Start(string[] args) {
    ShowSplash();
    
    var app = new CommandApp<InteractiveCliMeneCommand>(services)
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
    // todo: removed until vulnerability in package is resolved
    //    Error: Centazio.Cli.csproj: [NU1903] Warning As Error: Package 'SixLabors.ImageSharp' 3.1.5 has a known high severity vulnerability, https://github.com/advisories/GHSA-2cmq-823j-5qj8
    // AnsiConsole.Write(new CanvasImage(FsUtils.GetSolutionFilePath("src", "Centazio.Cli", "swirl.png")).MaxWidth(32));
    AnsiConsole.Write(new FigletText("Centazio").LeftJustified().Color(Color.Blue));
    UiHelpers.Log("[link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }

  public void ReportException(Exception ex, bool terminate) {
    Log.Error(ex, $"unhandled exception");
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShowLinks);
    if (terminate) Environment.Exit(-1);
  }

}