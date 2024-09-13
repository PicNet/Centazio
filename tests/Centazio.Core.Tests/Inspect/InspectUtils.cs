namespace Centazio.Core.Tests.Inspect;

internal static class InspectUtils {

  private static string? solndir;
  internal static string SolnDir => solndir ??= GetSolutionRootDirectory();
  
  private static List<string>? csfiles;
  internal static List<string> CsFiles(params string[] ignore) => (csfiles ??= GetSolnCsFiles()).Where(f => !ignore.Any(f.EndsWith) && !f.Contains("\\obj\\")).ToList();

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
  
  private static List<string> GetSolnCsFiles() => Directory.GetFiles(SolnDir, "*.cs", SearchOption.AllDirectories).ToList();

}