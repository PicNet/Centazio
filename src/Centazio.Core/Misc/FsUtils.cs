namespace Centazio.Core.Misc;

// todo: clean up all these methods with correct names
public static class FsUtils {
  
  private static string? devroot;
  private static string? cliinstall;
  
  public static string GetDevPath(params List<string> steps) {
    var file = "centazio3.sln";
    devroot ??= GetDevRootDir(Environment.CurrentDirectory) ?? throw new Exception($"failed to find the root directory by searching for [{file}]"); 
    return GetPathFromRootAndSteps(devroot, steps);
        
    string? GetDevRootDir(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return dir;

      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : GetDevRootDir(parent);
    }
  }
  
  internal static string TestingCliRootDir = String.Empty;

  public static string GetCliDir(params List<string> steps) {
    return GetPathFromRootAndSteps(cliinstall ??= GetCliRootDir(), steps);
    
    string GetCliRootDir() {
      if (Env.IsInDev()) return String.IsNullOrEmpty(TestingCliRootDir) ? GetDevPath() : TestingCliRootDir;

      var exe = Assembly.GetEntryAssembly() ?? throw new Exception();
      if (exe.GetName().Name != "Centazio.Cli") throw new Exception($"expected {nameof(GetCliDir)} to be called from Centazio.Cli context");
      var clidir = Path.GetDirectoryName(exe.Location) ?? throw new Exception("Could not find a valid templates directory");
      return Path.GetFullPath(Path.Combine(clidir, "..", "..", "..", "content"));
    }
  }
  
  public static string GetTemplatePath(params List<string> steps) => GetPathFromRootAndSteps(GetCliDir("defaults", "templates"), steps);

  // use this as a replacement to Directory.Delete(dir, true); as it will allow open directories to still be deleted
  public static void EmptyDirectory(string dir) {
    if (!Directory.Exists(dir)) {
      Directory.CreateDirectory(dir);
      return;
    }
    Directory.GetDirectories(dir).ForEach(d => Directory.Delete(d, true));
    Directory.GetFiles(dir).ForEach(File.Delete);
  }

  private static string GetPathFromRootAndSteps(string root, params List<string> steps) => 
      Path.GetFullPath(Path.Combine(steps.Prepend(root).ToArray()));

}