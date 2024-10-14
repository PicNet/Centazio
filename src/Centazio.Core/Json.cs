using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Centazio.Core;

public static class Json {
  
  public static string Serialize(object o) => JsonSerializer.Serialize(PrepareDtoForSerialisation(o));
  public static T Deserialize<T>(string json) {
    var dtot = GetDtoTypeFromTypeHierarchy(typeof(T));
    if (dtot is null) return JsonSerializer.Deserialize<T>(json) ?? throw new Exception();
    var dtoobj = JsonSerializer.Deserialize(json, dtot);
    var obj = Activator.CreateInstance(typeof(T)) ?? throw new Exception();
    var dtoprops = dtot.GetProperties();
    typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .ForEach(pi => {
          var dtopi = dtoprops.SingleOrDefault(pi2 => pi2.Name == pi.Name) ?? throw new Exception($"could not find property[{pi.Name}] in Dto[{dtot.FullName}]");
          pi.SetValue(obj, GetObjVal(pi, dtopi));
        });
    return (T) obj;
    
    object? GetObjVal(PropertyInfo origpi, PropertyInfo dtopi) {
      var dtoval = dtopi.GetValue(dtoobj);
      if (dtoval is null) return null;
      if (origpi.PropertyType.IsEnum) return Enum.Parse(origpi.PropertyType, (string) dtoval);
      if (origpi.PropertyType.IsAssignableTo(typeof(ValidString))) return Activator.CreateInstance(origpi.PropertyType, dtoval);
      return Convert.ChangeType(dtoval, origpi.PropertyType) ?? throw new Exception();
    }
  }

  public static bool AreJsonEqual(object actual, object expected) {
    var (actualjson, expjson) = (JsonSerializer.Serialize(actual), JsonSerializer.Serialize(expected));
    if (actualjson == expjson) return true;
    throw new Exception($"Expected json representations to be equivalent:\nActual  :{actualjson}\nExpected:{expjson}");
  }

  private static object PrepareDtoForSerialisation(object orig) {
    var dtot = GetDtoTypeFromTypeHierarchy(orig.GetType());
    if (dtot is null) return orig;
    var dto = Activator.CreateInstance(dtot) ?? throw new Exception();
    var dtoprops = dtot.GetProperties();
    orig.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .ForEach(pi => {
          var dtopi = dtoprops.SingleOrDefault(pi2 => pi2.Name == pi.Name) ?? throw new Exception($"could not find property[{pi.Name}] in Dto[{dtot.FullName}]");
          dtopi.SetValue(dto, GetDtoVal(pi, dtopi));
        });
    return dto;
    
    object? GetDtoVal(PropertyInfo origpi, PropertyInfo dtopi) {
      var origval = origpi.GetValue(orig);
      if (origval is null) return origval;
      if (origpi.PropertyType.IsEnum) return origpi.GetValue(orig)?.ToString();
      if (origpi.PropertyType.IsAssignableTo(typeof(ValidString))) return ((ValidString)origval).Value;   
      return Convert.ChangeType(origval, Nullable.GetUnderlyingType(dtopi.PropertyType) ?? dtopi.PropertyType) ?? throw new Exception();
    }
  }
  
  private static Type? GetDtoTypeFromTypeHierarchy(Type? baset) {
    while(baset is not null) {
      var t = baset.Assembly.GetType(baset.FullName + "+Dto");
      if (t is not null) return t;
      baset = baset.BaseType;
    }
    return null;
  }
}