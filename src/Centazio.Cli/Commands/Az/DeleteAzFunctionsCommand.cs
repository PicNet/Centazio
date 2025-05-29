using Centazio.Cli.Infra.Az;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeleteAzFunctionsCommand([FromKeyedServices("az")] CentazioSettings coresettings,  IAzFunctionDeleter impl, ITemplater templater) : AbstractCentazioCommand<DeleteAzFunctionsCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  public override async Task ExecuteImpl(Settings settings) {
    var project = new AzFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings, templater);
    
    if (!UiHelpers.Confirm($"Are you sure you want to delete Azure Function '{project.DashedProjectName}'")) {
      UiHelpers.Log("Aborting, no function deleted", LogEventLevel.Warning);
      return;
    }
    
    await UiHelpers.Progress($"Deleting Azure Function '{project.DashedProjectName}'", async () => await impl.Delete(project));
    UiHelpers.Log($"Azure Function App '{project.DashedProjectName}' successfully deleted.");
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
  }
}
