using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class GenerateAzFunctionsCommand(CentazioSettings coresettings, ITemplater templater) : AbstractCentazioCommand<GenerateAzFunctionsCommand.Settings> {

  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new AzureFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings.Defaults.GeneratedCodeFolder);
    
    await UiHelpers.Progress("Generating Azure Function project", async () => await new AzureCloudSolutionGenerator(coresettings, templater, project, settings.EnvironmentsList).GenerateSolution());
    await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
  }

}