using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Centazio.Core.Misc;

public class ValueObjectConverter<T> : JsonConverter<T> where T : class {

  public override T Read(ref Utf8JsonReader reader, Type target, JsonSerializerOptions opts) {
    if (reader.TokenType == JsonTokenType.String) return (T) (Activator.CreateInstance(target, reader.GetString()) ?? throw new Exception());
    
    if (reader.TokenType != JsonTokenType.StartObject)
      throw new JsonException("Expected StartObject token");

    var value = String.Empty;
    while (reader.Read()) {
      if (reader.TokenType == JsonTokenType.EndObject)
        break;

      if (reader.TokenType != JsonTokenType.PropertyName)
        continue;

      var prop = reader.GetString()!;
      if (prop == "Value") {
        reader.Read();
        value = reader.GetString()!;
      }

      reader.Skip(); // ignore unknown fields
    }

    return (T) (Activator.CreateInstance(target, value) ?? throw new Exception());
  }

  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
    writer.WriteStartObject();
    writer.WriteString("Value", value.ToString());
    writer.WriteEndObject();
  }
}


public static class Json {
  
  internal static readonly JsonSerializerOptions DEFAULT_OPTS = new() {
    RespectNullableAnnotations = true,
    PropertyNamingPolicy = null,
    // todo GT: use `ValidString.AllSubclasses()`
    Converters = {
      new ValueObjectConverter<SystemName>(), 
      new ValueObjectConverter<LifecycleStage>(), 
      new ValueObjectConverter<ObjectName>(),
      new ValueObjectConverter<ValidString>()
    }
  };
  
  internal static readonly JsonSerializerOptions HTTP_CONTENT_WRITE_OPTS = new(DEFAULT_OPTS) {
    Converters = { new SystemEntityAwareConverter() }
  };
  
  public static List<T> SplitList<T>(string json, string path) => 
      JsonStrToArray(json, path).Deserialize<List<T>>() ?? throw new Exception();
  
  public static List<string> SplitList(string json, string path) => 
      JsonStrToArray(json, path).Select(n => n?.ToJsonString() ?? String.Empty).ToList();

  private static JsonArray JsonStrToArray(string json, string path) {
    var node = JsonNode.Parse(json) ?? throw new Exception();
    return String.IsNullOrWhiteSpace(path) ? node.AsArray() : (JsonArray) NavigateNode(node, path);
  }

  public static string Serialize(object o) => JsonSerializer.Serialize(o, DEFAULT_OPTS);
  
  public static HttpContent SerializeToHttpContent(object o) {
    var json = JsonSerializer.Serialize(o, HTTP_CONTENT_WRITE_OPTS);
    return new StringContent(json, Encoding.UTF8, "application/json");
  }
  
  public static T Deserialize<T>(string json) => (T) Deserialize(json, typeof(T));
  
  public static object Deserialize(string json, Type type) {
    
    return DeserializeImpl(type);
    
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
    
  }

  public static bool ValidateJsonEquivalent(IEnumerable<object> a, IEnumerable<object> b, string aname="Actual", string bname="Expected") {
    return ValidateJsonEqual(
        a.Select(Serialize).OrderBy(s => s).ToList(), 
        b.Select(Serialize).OrderBy(s => s).ToList(), aname, bname);
  }
  
  public static bool ValidateJsonEqual(object? actual, object? expected, string aname="Actual", string bname="Expected") {
    var (actualjson, expjson) = (JsonSerializer.Serialize(actual, DEFAULT_OPTS), JsonSerializer.Serialize(expected, DEFAULT_OPTS));
    if (actualjson == expjson) return true;
    throw new Exception($"Expected json representations to be equivalent:\n{aname}: {actualjson}\n{bname}: {expjson}");
  }

  public static async Task<string> ReadFile(string file) => Regex.Replace(await File.ReadAllTextAsync(file), "// .*", String.Empty);
  public static async Task<Stream> ReadFileAsStream(string file) => new MemoryStream(Encoding.UTF8.GetBytes(await ReadFile(file)));

  
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
}

public class SystemEntityAwareConverter : JsonConverter<object> {

  private readonly Dictionary<string, PropertyInfo> IGNORE = ReflectionUtils.GetAllProperties<ISystemEntity>().ToDictionary(pi => pi.Name);
  
  public override bool CanConvert(Type type) => typeof(ISystemEntity).IsAssignableFrom(type);

  public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options) {
    if (value is null) { writer.WriteNullValue(); return; }

    writer.WriteStartObject();
    GetSerializableProperties(value.GetType()).ForEach(pv => {
      writer.WritePropertyName(GetJsonPropertyName(pv.Prop));
      JsonSerializer.Serialize(writer, pv.Value, options);
    });

    writer.WriteEndObject();

    List<(PropertyInfo Prop, object Value)> GetSerializableProperties(Type type) => type.GetProperties()
        .Where(p => !IGNORE.ContainsKey(p.Name) && p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .Select(p => (p, p.GetValue(value)))
        .OfType<(PropertyInfo, object)>()
        .ToList();
    
    string GetJsonPropertyName(PropertyInfo pi) => 
        pi.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name 
        ?? options.PropertyNamingPolicy?.ConvertName(pi.Name) 
        ?? pi.Name;
  }
  
  public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize(ref reader, typeToConvert, options);
}