using Centazio.Cli.Infra.Gen;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen.Centazio;

public class GenerateSlnCommand(ICentazioCodeGenerator gen) : AbstractCentazioCommand<GenerateSlnCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    SolutionName = UiHelpers.Ask("Solution Name"),
    CoreStorageProvider = UiHelpers.Ask("Select Core Storage Provider", "Sqlite")
  });

  public override async Task ExecuteImpl(Settings settings) {
    if (Directory.GetDirectories(".", settings.SolutionName).Any()) {
      UiHelpers.Log($"The current directory ({Environment.CurrentDirectory}) already contains a '{settings.SolutionName}' directory.  Please remove this '{settings.SolutionName}' directory and try again.");
      return;
    }
    if (Directory.GetFiles(".", "*.sln").Any() && !UiHelpers.Confirm($"The current directory ({Environment.CurrentDirectory}) appears to already contain a .Net solution.  Are you sure you want to proceed?")) return;
    if (Directory.GetFiles(".", "*.csproj").Any() && !UiHelpers.Confirm($"The current directory ({Environment.CurrentDirectory}) appears to be a .Net project.  Are you sure you want to proceed?")) return;
    
    var sln = await gen.GenerateSolution(settings.SolutionName, settings.CoreStorageProvider);
    
    UiHelpers.Log($"Solution '{sln}' generated in current directory ({Environment.CurrentDirectory}), please run `cd {sln}; centazio gen func <function_name>` to generate your first Centazio function");
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<SOLUTION_NAME>")] public required string SolutionName { get; init; }
    [CommandOption("-p|--provider")] public required string CoreStorageProvider { get; init; }
  }

}