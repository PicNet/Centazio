using System.ComponentModel;
using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployAzFunctionsCommand(CentazioSettings coresettings,  IAzFunctionDeployer impl, ICommandRunner cmd) : AbstractCentazioCommand<DeployAzFunctionsCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.Defaults.GeneratedCodeFolder);
    
    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await CloudSolutionGenerator.Create(coresettings, project, settings.Env).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings).PublishProject(project));
    
    await UiHelpers.Progress($"Deploying the Azure Function '{project.DashedProjectName}'", async () => await impl.Deploy(project));
    UiHelpers.Log($"Azure Function '{project.DashedProjectName}' deployed.");
    
    if (settings.ShowLogs) {
      UiHelpers.Log($"Attempting to connect to function log stream.");
      cmd.Func(coresettings.Parse(coresettings.Defaults.ConsoleCommands.Func.ShowLogStream, new { AppName = project.DashedProjectName }));
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
    [CommandOption("-l|--show-logs")] [DefaultValue(false)] public bool ShowLogs { get; set; }
  }
}
