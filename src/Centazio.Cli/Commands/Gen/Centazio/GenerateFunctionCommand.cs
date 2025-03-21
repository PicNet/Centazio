using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Misc;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen.Centazio;

public class GenerateFunctionCommand(ICommandRunner cmd, ITemplater templater) : AbstractCentazioCommand<GenerateFunctionCommand.Settings> {

  protected override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    FunctionName = UiHelpers.Ask("Function Name")
  });

  protected override Task ExecuteImpl(Settings settings) {
    if (Directory.GetDirectories(".", settings.FunctionName).Any()) {
      UiHelpers.Log($"The current directory already contains a '{settings.FunctionName}' directory.  Please remove this directory before you proceed.");
      return Task.CompletedTask;
    }
    
    var dir = Directory.CreateDirectory(settings.FunctionName);
    cmd.DotNet($"sln add {settings.FunctionName}", dir.FullName);
    
    var contents = templater.ParseFromPath("Solution.sln", new { Projects = 1 });
    Console.WriteLine(contents);
    return Task.CompletedTask;
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<FUNCTION_NAME>")] public required string FunctionName { get; init; }
    [CommandOption("-r|--read")] public bool Read { get; set; }
    [CommandOption("-p|--promote")] public bool Promote { get; set; }
    [CommandOption("-w|--write")] public bool Write { get; set; }
    [CommandOption("-o|--other")] public bool Other { get; set; }
  }

}