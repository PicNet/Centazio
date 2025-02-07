using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployAzFunctionsCommand(IAzFunctionDeployer impl) : AbstractCentazioCommand<DeployAzFunctionsCommand.Settings> {
  
  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new Settings { 
        AssemblyName = UiHelpers.Ask("Assembly Name"),
        FunctionName = UiHelpers.Ask("Function Class Name", "All"),
      });

  protected override async Task ExecuteImpl(Settings settings) { await impl.Deploy("todo: project path"); }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
    [CommandArgument(0, "<FUNCTION-NAME>")] public string? FunctionName { get; init; }
  }
}
