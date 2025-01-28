using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Centazio.Core.Types;
using Serilog;

namespace Centazio.Core.Misc;

public static class Json {
  
  internal static readonly JsonSerializerOptions DEFAULT_OPTS = new() {
    RespectNullableAnnotations = true
  };
  
  public static List<string> SplitList(string json, string path) {
    var node = JsonNode.Parse(json) ?? throw new Exception();
    
    var arr = String.IsNullOrWhiteSpace(path) ? node.AsArray() : (JsonArray) NavigateNode(node, path);
    return arr.Select(n => n?.ToJsonString() ?? String.Empty).ToList();
  }
  
  public static string Serialize(object o) => JsonSerializer.Serialize(DtoHelpers.HasDto(o) ? DtoHelpers.ToDto(o) : o, DEFAULT_OPTS);
  
  public static T Deserialize<T>(string json) => (T) Deserialize(json, typeof(T));
  
  public static object Deserialize(string json, Type type) {
    var dtot = DtoHelpers.GetDtoTypeFromTypeHierarchy(type);
    
    if (dtot is null) return DeserializeImpl(type);
    var dtoobj = DeserializeImpl(dtot);
    
    return TryCallIDtoGetBaseObj(out var baseobj) 
        ? baseobj 
        : SetAllBaseObjProps();

    object DeserializeImpl(Type targettype) {
      try { return JsonSerializer.Deserialize(json, targettype, DEFAULT_OPTS) ?? throw new Exception(); }
      catch (JsonException e) {
        Log.Warning($"could not deserialise entity of type [{type.Name}] with error:\n{e}\n\nfrom json:\n{PrettyPrint()}\n\n");
        throw;
      }
      
      string PrettyPrint() { 
        using var jDoc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(jDoc, new JsonSerializerOptions { WriteIndented = true });
      }
    }
    
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
    var (actualjson, expjson) = (JsonSerializer.Serialize(actual, DEFAULT_OPTS), JsonSerializer.Serialize(expected, DEFAULT_OPTS));
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

  private static JsonNode NavigateNode(JsonNode node, string path) {
    if (node is null || string.IsNullOrWhiteSpace(path)) throw new Exception();

    foreach (var step in path.Split('.')) {
      var arrstart = step.IndexOf('[');
      var arrend = step.IndexOf(']');

      if (arrstart != -1 && arrend != -1) {
        var prop = step[..arrstart];
        if (!Int32.TryParse(step.Substring(arrstart + 1, arrend - arrstart - 1), out var index)) throw new ArgumentException($"invalid array index in path: {step}");

        node = node[prop]?[index] ?? throw new Exception();
      }
      else {
        node = node[step] ?? throw new Exception();
      }
    }

    return node;
  }

  private record PropPair(PropertyInfo BasePi, PropertyInfo DtoPi);

}