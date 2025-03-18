using System.ComponentModel;
using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Misc;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AzFunctionLocalSimulateCommand(CentazioSettings coresettings, ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<AzFunctionLocalSimulateCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new AzureFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings.Defaults.GeneratedCodeFolder);

    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await new AzureCloudSolutionGenerator(coresettings, templater, project, settings.Environments.ToList()).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
    
    cmd.Func(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Func.LocalSimulateFunction), cwd: project.PublishPath, newwindow: true);
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
  }
}
