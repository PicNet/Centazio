﻿using System.Reflection;
using System.Text.Json.Serialization;

namespace Centazio.Core.Misc;

public static class ReflectionUtils {
  public static T GetPropVal<T>(object o, string prop) {
    var rprop = o.GetType().GetProperty(prop) ?? throw new Exception($"could not find property[{prop}] in type[{o.GetType().Name}]");
    return (T) (rprop.GetValue(o) ?? throw new Exception());
  }
  
  public static string GetPropValAsString(object o, string prop) {
    var rprop = o.GetType().GetProperty(prop) ?? throw new Exception($"could not find property[{prop}] in type[{o.GetType().Name}]");
    return rprop.GetValue(o)?.ToString() ?? throw new Exception();
  }
  
  public static bool IsRecord(Type t) => t.GetMethods().Any(m => m.Name == "<Clone>$");
  
  public static bool IsNullable(PropertyInfo p) {
    var nic = new NullabilityInfoContext().Create(p); 
    return Nullable.GetUnderlyingType(p.PropertyType) is not null ||
        nic.ReadState == NullabilityState.Nullable ||
        nic.WriteState == NullabilityState.Nullable;
  }
  
  public static bool IsJsonIgnore(Type t, string prop) => GetPropAttribute<JsonIgnoreAttribute>(t, prop) is not null;

  public static A? GetPropAttribute<A>(Type t, string prop) where A : Attribute {
    var p = t.GetProperty(prop);
    if (p is null) return null;
    if (p.GetCustomAttribute(typeof(A)) is A att) return att;
    if (p.PropertyType.GetCustomAttribute(typeof(A)) is A att2) return att2;
    var ifaceatt = t.GetInterfaces().Select(i => GetPropAttribute<A>(i, prop)).FirstOrDefault(a => a is not null); 
    if (ifaceatt is not null) return ifaceatt;
    return t.BaseType is not null ? GetPropAttribute<A>(t.BaseType, prop) : null;
  }
  
  public static string GetSolutionRootDirectory() {
    var file = "azure-pipelines.yml";

    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return dir;

      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : Impl(parent);
    }
    return Impl(Environment.CurrentDirectory) ?? throw new Exception("could not find the solution directory");
  }

}