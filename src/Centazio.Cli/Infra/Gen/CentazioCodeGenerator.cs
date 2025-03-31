namespace Centazio.Cli.Infra.Gen;

public interface ICentazioCodeGenerator {
  Task<string> GenerateSolution(string slnname);
  Task GenerateFunction(string slnfile, IFunctionGenerateSettings settings);
}

public class CentazioCodeGenerator(ICommandRunner cmd, ITemplater templater) : ICentazioCodeGenerator {

  public async Task<string> GenerateSolution(string slnname) {
    var (sln, shared, slndir) = (slnname, $"{slnname}.Shared", Directory.CreateDirectory(slnname).FullName);
    var shareddir = Path.Combine(slndir, shared);
    
    CreateSlnFile();
    CreateEmptySharedProj();
    CopySampleProjSharedProjFiles();
    await AdjustCopiedFiles();
    InstallRequiredNuGetPackages();
    
    return sln;

    void CreateSlnFile() { cmd.DotNet($"new sln --name {sln}", slndir); }

    void CreateEmptySharedProj() {
      var csproj = Path.Combine(shared, $"{shared}.csproj");
      cmd.DotNet($"new classlib --name {shared}", slndir);
      cmd.DotNet($"sln {sln}.sln add {csproj}", slndir);
      File.Delete(Path.Combine(slndir, shared, "Class1.cs"));
    }
    
    void CopySampleProjSharedProjFiles() {
      var from = FsUtils.GetTemplatesPath("centazio", "Solution.Shared");
      FsUtils.CopyDirFiles(from, shareddir, "*.*");
    }

    async Task AdjustCopiedFiles() {
      await Directory.GetFiles(shareddir, "*.cs").Select(async path => {
        var contents =  templater.ParseFromPath(path, new { Namespace = $"{sln}.Shared" });
        await File.WriteAllTextAsync(path, contents);
      }).Synchronous();
    }
    
    void InstallRequiredNuGetPackages() {
      cmd.DotNet("add package --prerelease Centazio.Core", Path.Combine(slndir, shared));
      cmd.DotNet("add package --prerelease Centazio.Providers.Sqlite", Path.Combine(slndir, shared));
    }
  }
  
  public async Task GenerateFunction(string slnfile, IFunctionGenerateSettings settings) {
    var sln = slnfile.Split('.').First();
    
    if (String.IsNullOrWhiteSpace(settings.AssemblyName)) {
      cmd.DotNet($"new classlib --name {settings.FunctionName}", Environment.CurrentDirectory);
      File.Delete(Path.Combine(settings.FunctionName, "Class1.cs"));
      cmd.DotNet($"sln {slnfile} add {settings.FunctionName}/{settings.FunctionName}.csproj", Environment.CurrentDirectory);
      
      cmd.DotNet("add package --prerelease Centazio.Core", settings.FunctionName);
      cmd.DotNet("add package --prerelease Centazio.Providers.Sqlite", settings.FunctionName);
      cmd.DotNet($"add reference ../{sln}.Shared", settings.FunctionName);
    }
    
    var from = FsUtils.GetTemplatesPath("centazio", "Functions");
    var files = new List<string> { "Assembly.cs", "SYSTEMApi.cs", "SYSTEM[MODE]Function.cs", "SYSTEMTypes.cs" };
    if (!String.IsNullOrWhiteSpace(settings.AssemblyName)) files.Add("SYSTEMIntegration.cs");
    await files.Select(async file => {
      var fromfile = file.Replace("[MODE]", settings.ModeName);
      var todir = String.IsNullOrWhiteSpace(settings.AssemblyName) ? settings.FunctionName : settings.AssemblyName;
      var tofile = fromfile.Replace("SYSTEM", settings.SystemName);
      var contents = templater.ParseFromPath(Path.Combine(from, fromfile), new {
        SharedProjectNamespace = $"{sln}.Shared", 
        Namespace = $"{sln}.{settings.SystemName}",
        SystemName = settings.SystemName
      }); 
      await File.WriteAllTextAsync(Path.Combine(todir, tofile), contents);
    }).Synchronous();
  }
}

public interface IFunctionGenerateSettings {
  string? AssemblyName { get; set; }
  string SystemName { get; }
  string ModeName { get; }
  string FunctionName { get; }
}
