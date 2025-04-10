namespace Centazio.Core.Misc;

public static class FsUtils {
  
  private static string? devroot;
  private static string? cliinstall;
  
  public static string GetDevPath(params List<string> steps) {
    devroot ??= TryToFindDirectoryOfFile("centazio3.sln") ?? throw new Exception($"failed to find the root dev directory"); 
    return GetPathFromRootAndSteps(devroot, steps);
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

  public static string? TryToFindDirectoryOfFile(string file, string? from = null) {
    var path = Path.Combine(from ?? Environment.CurrentDirectory, file);
    if (File.Exists(path)) return from;

    var parent = Directory.GetParent(from ?? Environment.CurrentDirectory)?.FullName;
    return parent is null ? null : TryToFindDirectoryOfFile(file, parent);
  }

}