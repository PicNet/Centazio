using Centazio.Cli.Infra.Ui;
using Centazio.Core.Misc;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Gen.Centazio;

public class GenerateSlnCommand(ICommandRunner cmd) : AbstractCentazioCommand<GenerateSlnCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    SolutionName = UiHelpers.Ask("Solution Name")
  });

  public override Task ExecuteImpl(Settings settings) {
    var done = Task.CompletedTask;
    if (Directory.GetDirectories(".", settings.SolutionName).Any()) {
      UiHelpers.Log($"The current directory ({Environment.CurrentDirectory}) already contains a '{settings.SolutionName}' directory.  Please remove this '{settings.SolutionName}' directory and try again.");
      return done;
    }
    if (Directory.GetFiles(".", "*.sln").Any() && !UiHelpers.Confirm($"The current directory ({Environment.CurrentDirectory}) appears to already contain a .Net solution.  Are you sure you want to proceed?")) return done;
    if (Directory.GetFiles(".", "*.csproj").Any() && !UiHelpers.Confirm($"The current directory ({Environment.CurrentDirectory}) appears to be a .Net project.  Are you sure you want to proceed?")) return done;
    
    var sln = GenerateCode(settings);
    
    UiHelpers.Log($"Solution '{sln}' generated in current directory ({Environment.CurrentDirectory}), please run `cd {sln}; centazio gen func <function_name>` to generate your first Centazio function");
    return done;
  }

  private string GenerateCode(Settings settings) {
    var (sln, shared, slndir) = (settings.SolutionName, $"{settings.SolutionName}.Shared", Directory.CreateDirectory(settings.SolutionName).FullName);
    var shareddir = Path.Combine(slndir, shared);
    
    CreateSlnFile();
    CreateEmptySharedProj();
    CopySampleProjSharedProjFiles();
    AdjustCopiedFiles();
    
    return sln;

    void CreateSlnFile() { cmd.DotNet($"new sln --name {sln}", slndir); }

    void CreateEmptySharedProj() {
      var csproj = Path.Combine(shared, $"{shared}.csproj");
      cmd.DotNet($"new classlib --name {shared}", slndir);
      cmd.DotNet($"sln {sln}.sln add {csproj}", slndir);
      File.Delete(Path.Combine(slndir, shared, "Class1.cs"));
    }
    
    void CopySampleProjSharedProjFiles() {
      // todo: change all `FsUtils.GetSolutionFilePath` in Centazio.Cli project to use `Templater.TemplatePath` so that it works using a global dotnet tool
      // or perhaps better yet, find a better place than Templater for this, as its not just templates that need root directory in the Cli project. 
      var from = Templater.TemplatePath("defaults", "templates", "centazio", "Solution.Shared");
      FsUtils.CopyDirFiles(from, shareddir, "*.cs");
    }
    
    void AdjustCopiedFiles() {
      Directory.GetFiles(shareddir, "*.cs").ForEach(path => {
        var fn = Path.GetFileName(path);
        var contents = File.ReadAllText(path).Replace("Centazio.Sample", sln);
        if (fn.Contains("Sample") || contents.Contains("Sample")) throw new Exception();
        File.WriteAllText(path, contents);
      });
    }
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<SOLUTION_NAME>")] public required string SolutionName { get; init; }
  }

}