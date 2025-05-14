namespace Centazio.Cli.Infra.Gen;

public interface ICentazioCodeGenerator {
  Task<string> GenerateSolution(string slnname, string? provider);
  Task GenerateFunction(string slnfile, IFunctionGenerateSettings settings);
}

public class CentazioCodeGenerator(ICommandRunner cmd, ITemplater templater, bool usenuget=true) : ICentazioCodeGenerator {

  public async Task<string> GenerateSolution(string slnname, string? provider) {
    if (!usenuget && !Env.IsInDev()) throw new Exception(nameof(CentazioCodeGenerator) + " should use NuGet packages if not running inside a Centazio dev directory");
      
    var (sln, shared, slndir) = (slnname, $"{slnname}.Shared", Directory.CreateDirectory(slnname).FullName);
    var shareddir = Path.Combine(slndir, shared);
    
    CreateSlnFile();
    CreateEmptySharedProj();
    CopySampleProjSharedProjFiles();
    await AdjustCopiedFiles();
    InstallCentazioNuGetsOrRefs(Path.Combine(slndir, shared), provider);
    return sln;
    
    void CreateSlnFile() { cmd.DotNet($"new sln --name {sln}", slndir); }

    void CreateEmptySharedProj() {
      var csproj = Path.Combine(shared, $"{shared}.csproj");
      cmd.DotNet($"new classlib --name {shared}", slndir);
      cmd.DotNet($"sln {sln}.sln add {csproj}", slndir);
      File.Delete(Path.Combine(slndir, shared, "Class1.cs"));
    }
    
    void CopySampleProjSharedProjFiles() {
      var from = FsUtils.GetTemplateDir("centazio", "Solution.Shared");
      Directory.GetFiles(from, "*.*").ForEach(file => File.Copy(file, Path.Combine(shareddir, Path.GetFileName(file))));
    }

    async Task AdjustCopiedFiles() {
      await Directory.GetFiles(shareddir, "*.cs").Select(async path => {
        var contents =  templater.ParseFromPath(path, new { Namespace = $"{sln}.Shared" });
        await File.WriteAllTextAsync(path, contents);
      }).Synchronous();
    }
  }
  
  public async Task GenerateFunction(string slnfile, IFunctionGenerateSettings settings) {
    var sln = slnfile.Split('.').First();
    
    if (String.IsNullOrWhiteSpace(settings.AssemblyName)) {
      cmd.DotNet($"new classlib --name {settings.FunctionName}", Environment.CurrentDirectory);
      File.Delete(Path.Combine(settings.FunctionName, "Class1.cs"));
      cmd.DotNet($"sln {slnfile} add {settings.FunctionName}/{settings.FunctionName}.csproj", Environment.CurrentDirectory);
      
      InstallCentazioNuGetsOrRefs(settings.FunctionName, null);
      cmd.DotNet($"add reference ../{sln}.Shared", settings.FunctionName);
    }
    
    var from = FsUtils.GetTemplateDir("centazio", "Functions");
    var files = new List<string> { "Assembly.cs", "SYSTEMApi.cs", "SYSTEM[MODE]Function.cs", "SYSTEMTypes.cs" };
    if (!String.IsNullOrWhiteSpace(settings.AssemblyName)) files.Add("SYSTEMIntegration.cs");
    await files.Select(async file => {
      var fromfile = file.Replace("[MODE]", settings.ModeName);
      var todir = String.IsNullOrWhiteSpace(settings.AssemblyName) ? settings.FunctionName : settings.AssemblyName;
      var tofile = fromfile.Replace("SYSTEM", settings.SystemName);
      var contents = templater.ParseFromPath(Path.Combine(from, fromfile), new {
        SharedProjectNamespace = $"{sln}.Shared", 
        Namespace = $"{sln}.{settings.SystemName}",
        settings.SystemName
      }); 
      await File.WriteAllTextAsync(Path.Combine(todir, tofile), contents);
    }).Synchronous();
  }
  
  private void InstallCentazioNuGetsOrRefs(string projdir, string? provider) {
    if (usenuget) InstallNuGets(); else InstallRefs();

    void InstallNuGets() {
      cmd.DotNet("add package --prerelease Centazio.Core", projdir);
      if (provider is not null) cmd.DotNet($"add package --prerelease Centazio.Providers.{provider}", projdir);
    }
    
    void InstallRefs() {
      cmd.DotNet($"add reference {FsUtils.GetCentazioPath("src", "Centazio.Core")}", projdir);
      if (provider is not null) cmd.DotNet($"add reference {FsUtils.GetCentazioPath("src", "Centazio.Providers", $"Centazio.Providers.{provider}")}", projdir);
    }
  }
}

public interface IFunctionGenerateSettings {
  string? AssemblyName { get; set; }
  string SystemName { get; }
  string ModeName { get; }
  string FunctionName { get; }
}
