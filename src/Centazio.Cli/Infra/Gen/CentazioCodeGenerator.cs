﻿namespace Centazio.Cli.Infra.Gen;

public interface ICentazioCodeGenerator {
  Task<string> GenerateSolution(string slnname, string? provider);
  Task GenerateFunction(string slnfile, IFunctionGenerateSettings settings);
}

public class CentazioCodeGenerator(ICommandRunner cmd, ITemplater templater, bool usenuget=true) : ICentazioCodeGenerator {

  public async Task<string> GenerateSolution(string slnname, string? provider) {
    var (sln, shared, slndir) = (slnname, $"{slnname}.Shared", Directory.CreateDirectory(slnname).FullName);
    var shareddir = Path.Combine(slndir, shared);
    
    await CreateSlnFile();
    await CreateEmptySharedProj();
    CopySampleProjSharedProjFiles();
    await AdjustCopiedFiles();
    await InstallCentazioNuGetsOrRefs(Path.Combine(slndir, shared), provider);
    return sln;

    async Task CreateSlnFile() { await cmd.DotNet($"new sln --name {sln}", slndir); }

    async Task CreateEmptySharedProj() {
      var csproj = Path.Combine(shared, $"{shared}.csproj");
      await cmd.DotNet($"new classlib --name {shared}", slndir);
      await cmd.DotNet($"sln {sln}.sln add {csproj}", slndir);
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
      await cmd.DotNet($"new classlib --name {settings.FunctionName}", Environment.CurrentDirectory);
      File.Delete(Path.Combine(settings.FunctionName, "Class1.cs"));
      await cmd.DotNet($"sln {slnfile} add {settings.FunctionName}/{settings.FunctionName}.csproj", Environment.CurrentDirectory);
      
      await InstallCentazioNuGetsOrRefs(settings.FunctionName, null);
      await cmd.DotNet($"add reference ../{sln}.Shared", settings.FunctionName);
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
  
  private async Task InstallCentazioNuGetsOrRefs(string projdir, string? provider) {
    if (usenuget) await InstallNuGets(); else await InstallRefs();

    async Task InstallNuGets() {
      await cmd.DotNet("add package --prerelease Centazio.Core", projdir);
      if (provider is not null) await cmd.DotNet($"add package --prerelease Centazio.Providers.{provider}", projdir);
    }

    async Task InstallRefs() {
      await cmd.DotNet($"add reference {FsUtils.GetCentazioPath("src", "Centazio.Core")}", projdir);
      if (provider is not null) await cmd.DotNet($"add reference {FsUtils.GetCentazioPath("src", "Centazio.Providers", $"Centazio.Providers.{provider}")}", projdir);
    }
  }
}

public interface IFunctionGenerateSettings {
  string? AssemblyName { get; set; }
  string SystemName { get; }
  string ModeName { get; }
  string FunctionName { get; }
}
