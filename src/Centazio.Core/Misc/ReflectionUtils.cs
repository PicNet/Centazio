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
}