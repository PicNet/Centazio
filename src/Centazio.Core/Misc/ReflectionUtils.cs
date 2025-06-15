using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Centazio.Core.Misc;

public static class ReflectionUtils {
  
  public static Assembly CENTAZIO_ASSEMBLY = typeof(ReflectionUtils).Assembly;

  public static T GetPropVal<T>(object o, string prop) {
    var rprop = o.GetType().GetProperty(prop) ?? throw new Exception($"could not find property[{prop}] in type[{o.GetType().Name}]");
    return (T)(rprop.GetValue(o) ?? throw new Exception());
  }

  public static string GetPropValAsString(object o, string prop) {
    var rprop = o.GetType().GetProperty(prop) ?? throw new Exception($"could not find property[{prop}] in type[{o.GetType().Name}]");
    return rprop.GetValue(o)?.ToString() ?? throw new Exception();
  }

  public static List<PropertyInfo> GetAllProperties<T>() {
    var flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance;
    if (!typeof(T).IsInterface) return [.. typeof(T).GetProperties(flags)];

    var props = new List<PropertyInfo>();

    var considered = new List<Type>();
    var queue = new Queue<Type>();
    considered.Add(typeof(T));
    queue.Enqueue(typeof(T));
    
    while (queue.Count > 0) {
      var subtype = queue.Dequeue();
      foreach (var iface in subtype.GetInterfaces()) {
        if (considered.Contains(iface)) continue;
        considered.Add(iface);
        queue.Enqueue(iface);
      }
      var typeprops = subtype.GetProperties(flags);
      var newpis = typeprops.Where(x => !props.Contains(x));
      props.InsertRange(0, newpis);
    }

    return props;
  }


  public static bool IsDefault(object val) => val.GetType().IsValueType && val.Equals(Activator.CreateInstance(val.GetType()));
  public static bool IsRecord(Type t) => t.GetMethods().Any(m => m.Name == "<Clone>$");

  public static bool IsNullable(PropertyInfo p) {
    var nic = new NullabilityInfoContext().Create(p);
    return Nullable.GetUnderlyingType(p.PropertyType) is not null ||
        nic.ReadState == NullabilityState.Nullable ||
        nic.WriteState == NullabilityState.Nullable;
  }

  public static bool IsJsonIgnore(Type t, string prop) {
    return GetPropAttribute<JsonIgnoreAttribute>(t, prop) is not null;
  }

  public static A? GetPropAttribute<A>(Type t, string prop) where A : Attribute {
    var p = t.GetProperty(prop);
    if (p?.GetCustomAttribute<A>(true) is { } att) return att;
    if (p?.PropertyType.GetCustomAttribute<A>(true) is { } att2) return att2;

    var ifaceatt = t.GetInterfaces().Select(i => GetPropAttribute<A>(i, prop)).FirstOrDefault(a => a is not null);
    if (ifaceatt is not null) return ifaceatt;

    return t.BaseType is not null ? GetPropAttribute<A>(t.BaseType, prop) : null;
  }
  
  public static List<Type> GetAllTypesThatImplement(Type t, List<string> allowed) {
    return allowed.SelectMany(assname => {
      var ass = LoadAssembly(assname);
      return GetAllTypesThatImplement(t, ass); 
    }).ToList(); 
  }
  
  public static List<Assembly> LoadAssembliesFuzzy(string pattern, List<string>? ignore = null) {
    ignore ??= [];
    var patterns = pattern.Split(' ', ',', ';', '|').Select(p => p.Trim()).Where(p => !String.IsNullOrEmpty(p)).ToList();
    var options = Directory.GetFiles(FsUtils.GetCentazioPath(), "*.dll", SearchOption.AllDirectories)
        .Where(MatchesPatterns)
        .ToList();
    var names = options.Select(dll => dll.Split(Path.DirectorySeparatorChar).Last().Replace(".dll", String.Empty)).Distinct().ToList();
    var suitables = names.Select(name => GetMostSuitableAssemblyToLoad(name, options)).ToList();
    return suitables.Select(Assembly.LoadFrom).ToList();
    
    bool MatchesPatterns(string dll) {
      if (ignore.Any(i => dll.IndexOf($"{Path.DirectorySeparatorChar}{i}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0)) return false;
      var name = dll.Split(Path.DirectorySeparatorChar).Last().Replace(".dll", String.Empty);
      return patterns.Any(p => name.EndsWith(p) || Regex.IsMatch(name, Regex.Escape(p).Replace("\\*", ".*")));
    }
  }
  
  public static Assembly LoadAssembly(string assembly) => 
      Assembly.LoadFrom(GetAssemblyPath(assembly));

  public static List<Assembly> GetProviderAssemblies() {
    return new DirectoryInfo(FsUtils.GetCentazioPath()).GetFiles("*.dll", SearchOption.AllDirectories)
        .Select(dll => dll.Name.Replace(".dll", String.Empty))
        .Where(ProviderAssemblyFilter)
        .Distinct()
        .Select(LoadAssembly)
        .ToList();
    
    bool ProviderAssemblyFilter(string? name) => name is not null && !name.EndsWith("Tests") && (name == $"{nameof(Centazio)}.{nameof(Core)}" || name.StartsWith($"{nameof(Centazio)}.Providers"));
  }

  public static string GetAssemblyPath(string assembly) {
    var dlls = Directory.GetFiles(FsUtils.GetCentazioPath(), "*.dll", SearchOption.AllDirectories).ToList();
    return GetMostSuitableAssemblyToLoad(assembly, dlls);
  }

  private static string GetMostSuitableAssemblyToLoad(string assemblynm, List<string> dlls) {
    var matching = dlls.Where(f => f.EndsWith($"{Path.DirectorySeparatorChar}{assemblynm}.dll")).ToList();
    if (!matching.Any()) throw new Exception($"could not find any matching assemblies matching the name [{assemblynm}.dll] in options:\n\t{String.Join("\n\t", dlls)}");
    
    if (Env.IsCloudHost) return matching.Single(); // cloud hosts will only have 1 copy of each dll
    
    var projdirnm = $"{Path.DirectorySeparatorChar}{assemblynm}{Path.DirectorySeparatorChar}";
    return matching.FirstOrDefault(path => path.IndexOf($"{projdirnm}bin{Path.DirectorySeparatorChar}Debug", StringComparison.Ordinal) >= 0)
            ?? matching.FirstOrDefault(path => path.IndexOf(projdirnm, StringComparison.Ordinal) >= 0)
            ?? matching.First();
  }

  public static List<Type> GetAllTypesThatImplement(Type t, Assembly assembly) {
    return assembly.GetExportedTypes()
        .Where(type => (type.FullName is not null && !type.IsAbstract && type.IsAssignableTo(t)) || IsDescendant(type))
        .ToList();

    bool IsDescendant(Type typ) =>
        (typ.IsGenericType ? typ.GetGenericTypeDefinition() : typ) == t || (typ.BaseType is not null && IsDescendant(typ.BaseType));
  }

  public static object ParseValue(object target, string path) {
    ArgumentNullException.ThrowIfNull(target);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    var curr = target;
    var segments = path.Split('.');

    foreach (var segment in segments) {
      var property = curr.GetType().GetProperty(segment) ?? throw new ArgumentException($"property {segment} not found");
      curr = property.GetValue(curr) ?? throw new ArgumentException($"property {segment} has null value");
    }

    return curr;
  }
  
  public static string ParseStrValue(object target, string path) => (string) ParseValue(target, path);

}
