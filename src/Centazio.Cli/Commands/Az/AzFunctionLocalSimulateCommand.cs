﻿using System.ComponentModel;
using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AzFunctionLocalSimulateCommand(CentazioSettings coresettings, ICommandRunner cmd) : AbstractCentazioCommand<AzFunctionLocalSimulateCommand.Settings> {
  
  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyName = UiHelpers.Ask("Assembly Name")
  });

  protected override async Task ExecuteImpl(string name, Settings settings) {
    var project = new FunctionProjectMeta(ReflectionUtils.LoadAssembly(settings.AssemblyName), ECloudEnv.Azure, coresettings.Defaults.GeneratedCodeFolder);

    if (!settings.NoGenerate) await UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await CloudSolutionGenerator.Create(coresettings, project, settings.Env).GenerateSolution());
    if (!settings.NoBuild) await UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings).PublishProject(project));
    
    Environment.CurrentDirectory = project.ProjectDirPath;
    cmd.Func(coresettings.Parse(coresettings.Defaults.ConsoleCommands.Func.LocalSimulateFunction), cwd: project.ProjectDirPath, newwindow:true);
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAME>")] public string AssemblyName { get; init; } = null!;
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
  }
}
