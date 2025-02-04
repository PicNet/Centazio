using Centazio.Cli.Infra.CodeGen.Csproj;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen;

public class GenerateFunctionsCommand(CentazioSettings coresettings) : AbstractCentazioCommand<GenerateFunctionsCommand.Settings> {

  protected override Task RunInteractiveCommandImpl() => ExecuteImpl(new() { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(Settings settings) {
    var ass = ReflectionUtils.LoadAssembly(settings.AssemblyName);
    await new ProjectGenerator(coresettings.GeneratedCodeFolder, ECloudEnv.Azure, ass).GenerateSolution();
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
  }

}