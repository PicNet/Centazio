using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
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
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.GeneratedCodeFolder);
    
    await ProjectGenerator.Create(project, "dev").GenerateSolution();
    await new DotNetCliProjectPublisher().PublishProject(project);
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
  }

}