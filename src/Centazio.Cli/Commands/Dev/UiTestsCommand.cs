using Centazio.Core.Misc;
using Centazio.Cli.Infra.Ui;
using Serilog.Events;
using Spectre.Console;

namespace Centazio.Cli.Commands.Dev;

public class UiTestsCommand : AbstractCentazioCommand<CommonSettings> {

  private const int DELAY_MS = 500; 
  
  protected override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  protected override async Task ExecuteImpl(string name, CommonSettings cmdsetts) {
    UiHelpers.Log($"[underline bold white]Running Ui Tests[/]\n\n");
    
    Title("UiHelpers.Log - Arguments Parsed");
    AnsiConsole.WriteLine(Json.Serialize(cmdsetts));
    
    Title("UiHelpers.Log - Levels");
    AnsiConsole.WriteLine("AnsiConsole.WriteLine");
    Enum.GetValues<LogEventLevel>().OrderBy(v => v).ForEach(v => UiHelpers.Log($"UiHelpers.Log({v})", v)); 
    
    Title($"UiHelpers.Progress");
    await UiHelpers.Progress("UiHelpers.Progress", () => Task.Delay(DELAY_MS));
    await UiHelpers.Progress($"[{RandCol()}]UiHelpers.Progress[/]", () => Task.Delay(DELAY_MS));
    await UiHelpers.ProgressWithErrorMessage($"[{RandCol()}]UiHelpers.ProgressWithErrorMessage[/]", async () => {
      await Task.Delay(DELAY_MS);
      return $"[{RandCol()}]error message[/]";
    });
    
    var rows = GetType().Assembly.GetExportedTypes()
        .Where(t => t is { IsAbstract: false, IsPublic: true } && typeof(ICentazioCommand).IsAssignableFrom(t))
        .Select(t => new List<string> { t.Namespace?.Replace("Centazio.Cli.Commands.", String.Empty) ?? String.Empty, t.Name })
        .OrderBy(row => row[0]).ThenBy(row => row[1])
        .Select(row => row.Select(v => $"[{RandCol()}]{v}[/]").ToList())
        .ToList();
    
    Title($"UiHelpers.Table");
    UiHelpers.Table([$"[{RandCol()}]Namespace[/]", $"[{RandCol()}]Command[/]"], rows);
    
    Title($"UiHelpers.Select");
    UiHelpers.Select("UiHelpers.Select:", rows.Select(row => String.Join('-', row)).ToList());
    
    Title($"UiHelpers.Ask");
    UiHelpers.Ask($"[{RandCol()}]Ask[/] (no default)");
    UiHelpers.Ask($"[{RandCol()}]Ask[/] (with default)", "default value");
    
    Title($"UiHelpers.AskForArr");
    UiHelpers.AskForArr($"[{RandCol()}]Ask[/] (no default)");
    UiHelpers.AskForArr($"[{RandCol()}]Ask[/] (with default)", ["value1", "value2"]);
    
    Title($"UiHelpers.Confirm");
    UiHelpers.Confirm($"[{RandCol()}]Confirm[/] (no default)");
    UiHelpers.Confirm($"[{RandCol()}]Confirm[/] (default true)", true);
    
    void Title(string title) => UiHelpers.Log($"\n[white]{title}[/]\n");
  }
  
  private Color RandCol() => Color.FromConsoleColor(Enum.GetValues<ConsoleColor>().Random());

}