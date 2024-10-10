using System.Reflection;

namespace Centazio.Core.Tests.Inspect;

internal static class InspectUtils {

  private static string? solndir;
  internal static string SolnDir => solndir ??= GetSolutionRootDirectory();
  
  private static List<string>? csfiles;
  public static List<string> CsFiles(string? dir, params string[] ignore) => (csfiles ??= GetSolnFiles(dir, "*.cs")).Where(f => !ignore.Any(f.EndsWith) && !f.Contains("\\obj\\")).ToList();
  
  public static List<string> GetCentazioDllFiles() {
    var centazios = GetSolnFiles(null, "*.dll")
        .Where(dll => dll.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) < 0 && dll.Split("\\").Last().IndexOf("Centazio", StringComparison.OrdinalIgnoreCase) >= 0)
        .ToList();
    var distinct = new Dictionary<string, (DateTime LastWrite, string Full)>();
    centazios.ForEach(file => {
      var (lastwrite, filename) = (File.GetLastWriteTime(file), file.Split("\\").Last());
      if (!distinct.ContainsKey(filename) || distinct[filename].LastWrite < lastwrite) 
        distinct[filename] = (lastwrite, file); 
    });
    return distinct.Values.Select(t => t.Full).ToList();
  }
  
  public static List<Assembly> LoadCentazioAssemblies() => GetCentazioDllFiles().Select(Assembly.LoadFrom).ToList();
  
  public static List<string> GetSolnFiles(string? dir, string extension) => Directory.GetFiles(dir ?? SolnDir, extension, SearchOption.AllDirectories).ToList();

  private static string GetSolutionRootDirectory() {
    var file = "azure-pipelines.yml";

    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return dir;

      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : Impl(parent);
    }

    return Impl(Environment.CurrentDirectory) ?? throw new Exception("could not find the solution directory");
  }
}