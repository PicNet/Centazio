using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen.Centazio;

public class GenerateFunctionCommand(ICommandRunner cmd) : AbstractCentazioCommand<GenerateFunctionCommand.Settings> {

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
    var sln = slnfile.Split('.').First();
    var files = new List<string> { "Assembly.cs", "ClickUpApi.cs", "ClickUp[MODE]Function.cs", "ClickUpTypes.cs" };
    if (!String.IsNullOrWhiteSpace(settings.AssemblyName)) files.Add("ClickUpIntegration.cs");
    
    if (String.IsNullOrWhiteSpace(settings.AssemblyName)) {
      cmd.DotNet($"new classlib --name {settings.FunctionName}", Environment.CurrentDirectory);
      File.Delete(Path.Combine(settings.FunctionName, "Class1.cs"));
      cmd.DotNet($"sln {slnfile} add {settings.FunctionName}/{settings.FunctionName}.csproj", Environment.CurrentDirectory);
      
      cmd.DotNet("add package --prerelease Centazio.Core", settings.FunctionName);
      cmd.DotNet("add package --prerelease Centazio.Providers.Sqlite", settings.FunctionName);
      cmd.DotNet($"add reference ../{sln}.Shared", settings.FunctionName);
    }
    
    var from = Templater.TemplatePath("defaults", "templates", "centazio", "Functions");
    await files.Select(async file => {
      var fromfile = file.Replace("[MODE]", settings.ModeName);
      var todir = String.IsNullOrWhiteSpace(settings.AssemblyName) ? settings.FunctionName : settings.AssemblyName;
      var tofile = fromfile.Replace("ClickUp", settings.SystemName);
      var contents = (await File.ReadAllTextAsync(Path.Combine(from, fromfile)))
          .Replace("ClickUp", settings.SystemName)
          .Replace("Centazio.Sample", sln);
      await File.WriteAllTextAsync(Path.Combine(todir, tofile), contents);
    }).Synchronous();
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<SYSTEM_NAME>")] public required string SystemName { get; init; }
    [CommandOption("-a|--assembly")] public string? AssemblyName { get; set; }
    [CommandOption("-r|--read")] public bool Read { get; set; }
    [CommandOption("-p|--promote")] public bool Promote { get; set; }
    [CommandOption("-w|--write")] public bool Write { get; set; }
    [CommandOption("-o|--other")] public bool Other { get; set; }
    
    internal string ModeName => Read ? nameof(Read) : Promote ? nameof(Promote) : Write ? nameof(Write) : nameof(Other);
    internal string FunctionName => SystemName + ModeName + "Function";
    
    public override ValidationResult Validate() {
      var results = base.Validate();
      if (!results.Successful) return results;
      var selected = new [] { Read, Promote, Write, Other }.Count(o => o);
      if (selected != 1) return ValidationResult.Error("The generate function command must have one of the flags set: --read, --promote, --write or --other");
      
      if (String.IsNullOrWhiteSpace(AssemblyName)) return results;
      if (!File.Exists($"{AssemblyName}.csproj")) return ValidationResult.Error($"Could not find the specified assembly file: '{AssemblyName}.csproj'");
      return results;
    } 

  }

}