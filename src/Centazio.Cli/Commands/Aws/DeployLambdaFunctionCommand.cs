using System.ComponentModel;
using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

// todo: implement
public class DeployLambdaFunctionCommand(CentazioSettings coresettings,  ILambdaFunctionDeployer impl, ICommandRunner cmd) : AbstractCentazioCommand<DeployLambdaFunctionCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() {
    var assnm = UiHelpers.Ask("Assembly Name");
    var settings = new Settings { AssemblyName = assnm, FunctionName = GetTargetFunction() }; 
    return Task.FromResult(settings);
  
    string GetTargetFunction() {
      var ass = ReflectionUtils.LoadAssembly(assnm);
      var options = IntegrationsAssemblyInspector.GetCentazioFunctions(ass, []).Select(f => f.Name).ToList();
      if (!options.Any()) throw new Exception($"Assembly '{assnm}' does not contain any Centazio Functions");
      if (options.Count == 1) return options.Single();
      return UiHelpers.PromptOptions("Select Function:", options);
    }
  }

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Aws, coresettings.Defaults.GeneratedCodeFolder);
    
    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Lambda Function project '{project.DashedProjectName}'", async () => await CloudSolutionGenerator.Create(coresettings, project, settings.Env).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings).PublishProject(project));
    
    await UiHelpers.Progress($"Deploying the Lambda Function '{project.DashedProjectName}'", async () => await impl.Deploy(project, settings.FunctionName));
    AnsiConsole.WriteLine($"Lambda Function '{project.DashedProjectName}' deployed.");
    
    if (settings.ShowLogs) {
      AnsiConsole.WriteLine($"Attempting to connect to function log stream.");
      cmd.Func(coresettings.Parse(coresettings.Defaults.ConsoleCommands.Lambda.ShowLogStream, new { AppName = project.DashedProjectName }));
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
    [CommandArgument(1, "<FUNCTION_NAME>")] public string FunctionName { get; init; } = null!;
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
    [CommandOption("-l|--show-logs")] [DefaultValue(false)] public bool ShowLogs { get; set; }
  }
}
