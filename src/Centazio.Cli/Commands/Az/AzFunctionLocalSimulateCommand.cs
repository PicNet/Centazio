using System.ComponentModel;
using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

// todo: support simulating multiple functions
// todo: support simulating only a single function in an assembly
// todo: simulator is loading aws secrets
public class AzFunctionLocalSimulateCommand(CentazioSettings coresettings, ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<AzFunctionLocalSimulateCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  public override async Task ExecuteImpl(Settings settings) {
    var project = new AzFunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), coresettings, templater);

    // todo: move command to ConsoleCommands
    cmd.Run("azurite", "--silent --inMemoryPersistence", newwindow: true);
    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await new AzCloudSolutionGenerator(coresettings, templater, project, settings.EnvironmentsList).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
    
    // todo: do we need newwindow in Windows?  We dont in Linux so removed here, but may need to change to `newwindow: !Env.IsLinux`
    // todo: make this a hidden window, does not need to pop up.  The CommandRunner should accept an Options object instead of adding more and more params
    cmd.Func(templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Func.LocalSimulateFunction), cwd: project.PublishPath);
  }
  
  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
  }
}
