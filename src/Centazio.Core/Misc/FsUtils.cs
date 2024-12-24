namespace Centazio.Core.Misc;

public static class FsUtils {
  
  public static string GetSolutionRootDirectory() {
    var file = "centazio3.sln";

    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return dir;

      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : Impl(parent);
    }
    return Impl(Environment.CurrentDirectory) ?? throw new Exception("could not find the solution directory");
  }
  
  public static string GetSolutionFilePath(params string[] steps) {
    var path = Path.Combine(new[] { GetSolutionRootDirectory() }.Concat(steps).ToArray());
    return !Path.Exists(path) ? throw new FileNotFoundException(path) : path;
  }

  public static string FindFirstValidDirectory(List<string> directories) => directories.FirstOrDefault(dir => {
      try { return Directory.Exists(dir); }
      catch { return false; }
    }) ?? throw new Exception($"Could not find a valid directory");
}