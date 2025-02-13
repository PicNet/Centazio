namespace Centazio.Core.Misc;

public static class FsUtils {
  
  private static string? rootdir;
  
  public static string GetSolutionRootDirectory() {
    if (rootdir is not null) return rootdir;
    if (CloudUtils.IsCloudEnviornment()) return rootdir = Environment.CurrentDirectory;
    
    // todo: change to settings.json or other more generic file (that exists in cloud) and test
    var file = "centazio3.sln";

    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return dir;

      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : Impl(parent);
    }
    return rootdir 
        ??= Impl(Environment.CurrentDirectory)
        ?? throw new Exception($"failed to find the root directory by searching for [{file}]");
  }
  
  public static string GetSolutionFilePath(params string[] steps) => 
      Path.Combine(steps.Prepend(GetSolutionRootDirectory()).ToArray());

  public static string FindFirstValidDirectory(List<string> directories) => directories.FirstOrDefault(dir => {
      try { return Directory.Exists(dir); }
      catch { return false; }
    }) ?? throw new Exception($"Could not find a valid directory");
}