namespace Centazio.Core.Misc;

public static class FsUtils {
  
  private static string? rootdir;
  
  public static string GetSolutionRootDirectory() {
    if (rootdir is not null) return rootdir;
    if (Env.IsCloudEnviornment()) return rootdir = Environment.CurrentDirectory;
    
    var file = "settings.json";

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
  
  public static string GetSolutionFilePath(params List<string> steps) => 
      Path.Combine(steps.Prepend(GetSolutionRootDirectory()).ToArray());

  public static string FindFirstValidDirectory(List<string> directories) => directories.FirstOrDefault(dir => {
      try { return Directory.Exists(dir); }
      catch { return false; }
    }) ?? throw new Exception($"Could not find a valid directory");
  
  public static void CopyDirFiles(string from, string to, string ext, bool deleteto = false) {
    if (deleteto) {
      if (Directory.Exists(to)) Directory.Delete(to, true);
      Directory.CreateDirectory(to);
    }
    Directory.GetFiles(from, ext).ForEach(file => File.Copy(file, Path.Combine(to, Path.GetFileName(file))));
  }
}