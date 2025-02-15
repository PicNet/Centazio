using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployAzFunctionsCommand(CentazioSettings coresettings,  IAzFunctionDeployer impl) : AbstractCentazioCommand<DeployAzFunctionsCommand.Settings> {
  
  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new Settings { 
        AssemblyName = UiHelpers.Ask("Assembly Name"),
        FunctionName = UiHelpers.Ask("Function Class Name", "All"),
      });

  protected override async Task ExecuteImpl(Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.Defaults.GeneratedCodeFolder);
    
    await UiHelpers.Progress("Generating Azure FunctionApp project", async () => await CloudSolutionGenerator.Create(coresettings, project, "dev").GenerateSolution());
    await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings).PublishProject(project));
    await UiHelpers.Progress("Deploying the FunctionApp to Azure", async () => await impl.Deploy(project)); 
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
    [CommandArgument(0, "<FUNCTION-NAME>")] public string? FunctionName { get; init; }
  }
}
