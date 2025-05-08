using Centazio.Cli.Infra.Gen;
using Centazio.Cli.Infra.Ui;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen.Centazio;

public class GenerateFunctionCommand(ICentazioCodeGenerator gen) : AbstractCentazioCommand<GenerateFunctionCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    SystemName = UiHelpers.Ask("Function Name")
  });

  public override async Task ExecuteImpl(Settings settings) {
    settings.Validate();
    var slns = Directory.GetFiles(".", "*.sln"); 
    if (slns.Length != 1) {
      UiHelpers.Log($"The current directory does not contain a sln file.  Please run the `centazio gen func` command from a valid .Net solution directory.");
      return;
    }
    if (Directory.GetDirectories(".", settings.FunctionName).Any()) {
      UiHelpers.Log($"The current directory already contains a '{settings.FunctionName}' directory.  Please remove this directory before you proceed.");
      return;
    }
    
    var slnfile = Path.GetFileName(slns.Single());
    await gen.GenerateFunction(slnfile, settings);
  }

  public class Settings : CommonSettings, IFunctionGenerateSettings {
    [CommandArgument(0, "<SYSTEM_NAME>")] public required string SystemName { get; init; }
    [CommandOption("-a|--assembly")] public string? AssemblyName { get; set; }
    [CommandOption("-r|--read")] public bool Read { get; set; }
    [CommandOption("-p|--promote")] public bool Promote { get; set; }
    [CommandOption("-w|--write")] public bool Write { get; set; }
    [CommandOption("-o|--other")] public bool Other { get; set; }
    
    public string ModeName => Read ? nameof(Read) : Promote ? nameof(Promote) : Write ? nameof(Write) : nameof(Other);
    public string FunctionName => SystemName + ModeName + "Function";
    
    public override ValidationResult Validate() {
      var results = base.Validate();
      if (!results.Successful) return results;
      var selected = new [] { Read, Promote, Write, Other }.Count(o => o);
      if (selected != 1) return ValidationResult.Error("The generate function command must have one of the flags set: --read, --promote, --write or --other");
      
      if (String.IsNullOrWhiteSpace(AssemblyName)) return results;
      if (!File.Exists($"{AssemblyName}\\{AssemblyName}.csproj")) return ValidationResult.Error($"Could not find the specified assembly file: '{AssemblyName}.csproj'");
      return results;
    } 

  }

}