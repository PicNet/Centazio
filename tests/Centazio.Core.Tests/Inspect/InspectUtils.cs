using NetArchTest.Rules;

namespace Centazio.Core.Tests.Inspect;

internal static class InspectUtils {
  private static readonly char SEP = Path.DirectorySeparatorChar;
  
  public static List<string> CsFiles(string? dir, params List<string> ignore) => GetSolnFiles(dir, "*.cs")
      .Where(f => !ignore.Any(f.EndsWith) && !f.Contains($"{SEP}obj{SEP}"))
      .ToList();
  
  public static List<string> GetCentazioDllFiles() {
    var centazios = GetSolnFiles(null, "*.dll")
        .Where(dll => {
          // load the assembly from the correct dll file, i.e. from the correct project
          var fn = dll.Split(SEP).Last();
          return fn.IndexOf("Centazio", StringComparison.Ordinal) >= 0 && 
              dll.IndexOf($"{fn.Replace(".dll", String.Empty)}{SEP}bin{SEP}Debug", StringComparison.Ordinal) >= 0;
        })
        .ToList();
    var distinct = new Dictionary<string, (DateTime LastWrite, string Full)>();
    centazios.ForEach(file => {
      var (lastwrite, filename) = (File.GetLastWriteTime(file), file.Split(SEP).Last());
      if (!distinct.ContainsKey(filename) || distinct[filename].LastWrite < lastwrite) 
        distinct[filename] = (lastwrite, file); 
    });
    return distinct.Values.Select(t => t.Full).ToList();
  }
  
  public static List<Assembly> LoadCentazioAssemblies() => GetCentazioDllFiles().Select(Assembly.LoadFrom).ToList();
  
  public static List<string> GetSolnFiles(string? dir, string extension) {
    var include = new List<string> { "src", "tests" };
    return Directory.GetFiles(dir ?? FsUtils.GetCentazioPath(), extension, SearchOption.AllDirectories)
        .Where(f => include.Any(d => f.Contains($"{Path.DirectorySeparatorChar}{d}{Path.DirectorySeparatorChar}")))
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
        .ToList();
  }

  public static readonly Types CentazioTypes = Types.InAssembly(ReflectionUtils.CENTAZIO_ASSEMBLY);
}
