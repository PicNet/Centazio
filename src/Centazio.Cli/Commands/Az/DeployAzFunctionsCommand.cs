﻿using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class DeployAzFunctionsCommand(CentazioSettings coresettings,  IAzFunctionDeployer impl) : AbstractCentazioCommand<DeployAzFunctionsCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.Defaults.GeneratedCodeFolder);
    
    await UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await CloudSolutionGenerator.Create(coresettings, project, settings.Env).GenerateSolution());
    await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings).PublishProject(project));
    await UiHelpers.Progress($"Deploying the Azure Function '{project.DashedProjectName}'", async () => await impl.Deploy(project)); 
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
  }
}
