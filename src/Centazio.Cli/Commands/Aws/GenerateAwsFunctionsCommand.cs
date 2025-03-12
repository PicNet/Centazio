using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class GenerateAwsFunctionsCommand(CentazioSettings coresettings, ITemplater templater) : AbstractCentazioCommand<GenerateAwsFunctionsCommand.Settings> {

  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name"),
    FunctionName = UiHelpers.Ask("Function Name"),
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Aws, coresettings.Defaults.GeneratedCodeFolder, settings.FunctionName);
    
    await UiHelpers.Progress("Generating AWS Lambda Function project", async () => await CloudSolutionGenerator.Create(coresettings, templater, project, settings.Env).GenerateSolution());
    await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project));
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public required string AssemblyName { get; init; }
    [CommandArgument(1, "<FUNCTION_NAME>")] public required string FunctionName { get; init; }
  }

}