using System.Reflection;
using System.Text.Json.Serialization;

namespace Centazio.Core.Misc;

public static class ReflectionUtils {

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
    if (p?.GetCustomAttribute(typeof(A), true) is A att) return att;
    if (p?.PropertyType.GetCustomAttribute(typeof(A)) is A att2) return att2;

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
  
  public static Assembly LoadAssembly(string assembly) => Assembly.LoadFrom(GetAssemblyPath(assembly));
  
  public static List<Assembly> GetProviderAssemblies() => 
      new DirectoryInfo(FsUtils.GetSolutionRootDirectory()).GetFiles("*.dll", SearchOption.AllDirectories)
          .Select(dll => dll.Name.Replace(".dll", String.Empty))
          .Where(name => name.StartsWith("Centazio.Providers.") && !name.Contains("Tests"))
          .Distinct()
          .Select(LoadAssembly)
          .ToList();

  public static string GetAssemblyPath(string assembly) {
    var fname = $"{assembly}.dll";
    var dlls = Directory.GetFiles(FsUtils.GetSolutionRootDirectory(), "*.dll", SearchOption.AllDirectories).Where(dll => dll.EndsWith(fname)).ToList();
    var assfile = 
        dlls.FirstOrDefault(path => path.IndexOf($"{assembly}{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug", StringComparison.Ordinal) >= 0)
        ?? dlls.FirstOrDefault(path => path.IndexOf($"{assembly}{Path.DirectorySeparatorChar}", StringComparison.Ordinal) >= 0)
        ?? throw new FileNotFoundException($"File for assembly [{assembly}] could not be found in directory (recursively) [{FsUtils.GetSolutionRootDirectory()}]");
    return assfile;
  }

  public static List<Type> GetAllTypesThatImplement(Type t, Assembly assembly) {
    return assembly.GetExportedTypes()
        .Where(type => (type.FullName is not null && !type.IsAbstract && type.IsAssignableTo(t)) || IsDescendant(type))
        .ToList();

    bool IsDescendant(Type typ) =>
        (typ.IsGenericType ? typ.GetGenericTypeDefinition() : typ) == t || (typ.BaseType is not null && IsDescendant(typ.BaseType));
  }
}
