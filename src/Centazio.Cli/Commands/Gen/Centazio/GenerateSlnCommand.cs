using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen.Centazio;

public class GenerateSlnCommand(ICommandRunner cmd) : AbstractCentazioCommand<GenerateSlnCommand.Settings> {

  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    SolutionName = UiHelpers.Ask("Solution Name")
  });

  // todo: why do we have this `name` parameter here?
  protected override Task ExecuteImpl(Settings settings) {
    var done = Task.CompletedTask;
    if (Directory.GetDirectories(".", settings.SolutionName).Any()) {
      UiHelpers.Log($"The current directory already contains a '{settings.SolutionName}' directory.  Please remove this directory before you proceed.");
      return done;
    }
    if (Directory.GetFiles(".", "*.sln").Any() && !UiHelpers.Confirm("The current directory appears to already contain a .Net solution.  Are you sure you want to proceed?")) return done;
    if (Directory.GetFiles(".", "*.csproj").Any() && !UiHelpers.Confirm("The current directory appears to be a .Net project.  Are you sure you want to proceed?")) return done;
    
    var dir = Directory.CreateDirectory(settings.SolutionName);
    cmd.DotNet($"new sln --name {settings.SolutionName}", dir.FullName);
    UiHelpers.Log($"Solution '{settings.SolutionName}' generated, please run `cd {settings.SolutionName}` and generate your first Centazio function with the `centazio gen func <function_name>");
    return done;
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<SOLUTION_NAME>")] public required string SolutionName { get; init; }
  }

}