using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Centazio.Core;

public static class Json {
  
  public static string Serialize(object o) => JsonSerializer.Serialize(PrepareDtoForSerialisation(o));
  public static T Deserialize<T>(string json) => (T) Deserialize(json, typeof(T));
  public static object Deserialize(string json, Type type) {
    var dtot = GetDtoTypeFromTypeHierarchy(type);
    if (dtot is null) return JsonSerializer.Deserialize(json, type) ?? throw new Exception();
    
    var dtoobj = JsonSerializer.Deserialize(json, dtot);
    return TryCallIDtoGetBaseObj(out var baseobj) 
        ? baseobj 
        : SetAllBaseObjProps();

    bool TryCallIDtoGetBaseObj(out object result) {
      result = new object();
      var iface = dtot.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDto<>));
      if (iface is null) return false; 
      var method = iface.GetMethod(nameof(IDto<object>.ToBase));
      if (method is null) return false;
      
      result = method.Invoke(dtoobj, []) ?? throw new Exception();
      return true;
    }

    object SetAllBaseObjProps() {
      var obj = Activator.CreateInstance(type) ?? throw new Exception();
      var pairs = GetPropPairs(type, dtot);
      pairs.ForEach(p => p.BasePi.SetValue(obj, GetObjVal(p.BasePi, p.DtoPi)));
      return obj;
      
      object? GetObjVal(PropertyInfo origpi, PropertyInfo dtopi) {
        var dtoval = dtopi.GetValue(dtoobj);
        if (dtoval is null) return null;
        if (origpi.PropertyType.IsEnum) return Enum.Parse(origpi.PropertyType, (string) dtoval);
        if (origpi.PropertyType.IsAssignableTo(typeof(ValidString))) return Activator.CreateInstance(origpi.PropertyType, dtoval);
        return Convert.ChangeType(dtoval, origpi.PropertyType) ?? throw new Exception();
      }
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
    var pairs = GetPropPairs(orig.GetType(), dtot);
    pairs.ForEach(p => p.DtoPi.SetValue(dto, GetDtoVal(p)));
    return dto;
    
    object? GetDtoVal(PropPair p) {
      var origval = p.BasePi.GetValue(orig);
      if (origval is null) return origval;
      if (p.BasePi.PropertyType.IsEnum) return p.BasePi.GetValue(orig)?.ToString();
      if (p.BasePi.PropertyType.IsAssignableTo(typeof(ValidString))) return ((ValidString)origval).Value;   
      return Convert.ChangeType(origval, Nullable.GetUnderlyingType(p.DtoPi.PropertyType) ?? p.DtoPi.PropertyType) ?? throw new Exception();
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
  
  private static List<PropPair> GetPropPairs(Type baset, Type dtot) {
    var dtoprops = dtot.GetProperties();
    return baset.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .Select(pi => {
          var dtopi = dtoprops.SingleOrDefault(pi2 => pi2.Name == pi.Name) ?? throw new Exception($"could not find property[{pi.Name}] in Dto[{dtot.FullName}]");
          return new PropPair(pi, dtopi);
        }).ToList();
  } 
  
  record PropPair(PropertyInfo BasePi, PropertyInfo DtoPi);
}