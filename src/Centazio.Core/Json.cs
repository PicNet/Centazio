using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Centazio.Core;

public static class Json {
  
  public static string Serialize(object o) => JsonSerializer.Serialize(DtoHelpers.HasDto(o) ? DtoHelpers.ToDto(o) : o);
  public static T Deserialize<T>(string json) => (T) Deserialize(json, typeof(T));
  public static object Deserialize(string json, Type type) {
    var dtot = DtoHelpers.GetDtoTypeFromTypeHierarchy(type);
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

  public static bool ValidateJsonEqual(IEnumerable<object> a, IEnumerable<object> b, string aname="Actual", string bname="Expected") {
    return ValidateJsonEqual((object) 
        a.Select(Serialize).OrderBy(s => s).ToList(), 
        b.Select(Serialize).OrderBy(s => s).ToList(), aname, bname);
  }
  public static bool ValidateJsonEqual(object? actual, object? expected, string aname="Actual", string bname="Expected") {
    var (actualjson, expjson) = (JsonSerializer.Serialize(actual), JsonSerializer.Serialize(expected));
    if (actualjson == expjson) return true;
    throw new Exception($"Expected json representations to be equivalent:\n{aname}: {actualjson}\n{bname}: {expjson}");
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