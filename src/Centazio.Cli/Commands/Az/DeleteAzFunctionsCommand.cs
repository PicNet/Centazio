using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeleteAzFunctionsCommand(CentazioSettings coresettings,  IAzFunctionDeleter impl) : AbstractCentazioCommand<DeleteAzFunctionsCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.Defaults.GeneratedCodeFolder);
    
    if (!UiHelpers.Confirm($"Are you sure you want to delete Azure Function '{project.DashedProjectName}'")) {
      AnsiConsole.WriteLine("Aborting, no function deleted");
      return;
    }
    
    await UiHelpers.Progress($"Deleting Azure Function '{project.DashedProjectName}'", async () => await impl.Delete(project));
    AnsiConsole.WriteLine($"Azure Function App '{project.DashedProjectName}' successfully deleted.");
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
  }
}
