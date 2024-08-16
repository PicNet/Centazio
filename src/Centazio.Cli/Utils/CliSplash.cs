using Spectre.Console;

namespace Centazio.Cli.Utils;

public interface ICliSplash {
  void Show();
}
  
public class CliSplash : ICliSplash {
  public void Show() {
    AnsiConsole.Write(new CanvasImage("swirl.png").MaxWidth(32));
    AnsiConsole.Write(new FigletText("Centazio").LeftJustified().Color(Color.Blue));
    AnsiConsole.MarkupLine("[link=https://picnet.com.au/application-integration-services/][underline blue]Centazio[/][/] by [link=https://picnet.com.au][underline blue]PicNet[/][/]\n\n");
  }
}