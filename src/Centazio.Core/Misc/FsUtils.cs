namespace Centazio.Core.Misc;

public static class FsUtils {
  
  internal static string TestingCliRootDir = String.Empty;
  internal static readonly string TEST_DEV_FILE = "centazio3.sln";
  
  private static readonly Lazy<string> devroot = new(GetDevRootImpl);
  private static readonly Lazy<string> cliroot = new(GetCliRootImpl);
  
  /// <summary>
  /// This method returns the path of the specified file (in steps) from a root determined on the application running context.
  /// - If the application is running from the dev directory or its children then the root is the directory containing the `sln` file
  /// - If the application is running as a Cli (and outside of the dev directory) then it uses the Cli root directory which is the
  ///     `dotnet tool install` - `content` directory
  /// - Otherwise we assume we are running under a hosting context such as Aws, Azure and then use the CWD 
  /// </summary>
  /// <param name="steps">The steps from the root to the file/directory</param>
  /// <returns>The absolute path to the specified file/directory</returns>
  public static string GetCentazioPath(params List<string> steps) => 
      Env.IsInDev ? GetDevPath(steps) : 
      Env.IsCli ? GetCliPath(steps) : 
      GetPathFromRootAndSteps(Environment.CurrentDirectory, steps);

  public static string GetDefaultsDir(params List<string> steps) => 
      GetPathFromRootAndSteps(GetCentazioPath("defaults"), steps);
  
  public static string GetTemplateDir(params List<string> steps) => 
      GetPathFromRootAndSteps(GetDefaultsDir("templates"), steps);

  /// <summary>
  /// Use this as a replacement to Directory.Delete(dir, true); as it will allow open
  ///     directories (in explorer for instance) to still be deleted 
  /// </summary>
  /// <param name="dir">Directory to empty</param>
  public static void EmptyDirectory(string dir) {
    if (!Directory.Exists(dir)) {
      Directory.CreateDirectory(dir);
      return;
    }
    Directory.GetDirectories(dir).ForEach(d => Directory.Delete(d, true));
    Directory.GetFiles(dir).ForEach(File.Delete);
  }

  /// <summary>
  /// Attempts to find the directory of the provided file, from the specified root (from), or the CWD.  This
  ///   method will check the entire directory hierarchy from the root up all its parents.
  /// </summary>
  /// <param name="file">The file to find in the root's path parent hierarchy</param>
  /// <param name="from">The starting root, if not specified then CWD is used</param>
  /// <returns>The directory containing the specified file, or null if the file is not found.</returns>
  public static string? FindFileDirectory(string file, string? from = null) {
    var path = Path.Combine(from ?? Environment.CurrentDirectory, file);
    if (File.Exists(path)) return from ?? Environment.CurrentDirectory;

    var parent = Directory.GetParent(from ?? Environment.CurrentDirectory)?.FullName;
    return parent is null ? null : FindFileDirectory(file, parent);
  }
  
  private static string GetDevPath(params List<string> steps) => 
      GetPathFromRootAndSteps(devroot.Value, steps);
  
  private static string GetDevRootImpl() => 
      FindFileDirectory(TEST_DEV_FILE) ?? throw new Exception($"failed to find the root dev directory"); 

  private static string GetCliPath(params List<string> steps) => 
      GetPathFromRootAndSteps(cliroot.Value, steps);

  private static string GetCliRootImpl() {
    if (!String.IsNullOrWhiteSpace(TestingCliRootDir)) return TestingCliRootDir;
    var exe = Assembly.GetEntryAssembly() ?? throw new Exception();
    if (exe.GetName().Name != "Centazio.Cli") throw new Exception($"expected {nameof(GetCliPath)} to be called from Centazio.Cli context");
    var clidir = Path.GetDirectoryName(exe.Location) ?? throw new Exception("Could not find a valid templates directory");
    // if the current Cli assembly is actually inside the dev directory,
    //    then just use the dev root directory, otherwise use the embedded 'content' directory
    var cliindev = FindFileDirectory(TEST_DEV_FILE, clidir);
    return Path.GetFullPath(cliindev ?? Path.Combine(clidir, "..", "..", "..", "content"));
  }

  private static string GetPathFromRootAndSteps(string root, params List<string> steps) => 
      Path.GetFullPath(Path.Combine(steps.Prepend(root).ToArray()));

}