using System.Text.Json;
using System.Text.Json.Serialization;

namespace Centazio.Core;

public static class Json {
  
  private static readonly JsonSerializerOptions opts = CreateDefaultOpts();
  
  internal static JsonSerializerOptions CreateDefaultOpts() {
    var o = new JsonSerializerOptions();
    ValidString.AllSubclasses().ForEach(t => {
      var convtyp = typeof(ValidStringJsonConverter<>).MakeGenericType(t);
      var converter = Activator.CreateInstance(convtyp) ?? throw new Exception();
      o.Converters.Add((JsonConverter) converter);
    });
    return o;
  }

  public static string Serialize(object o) => SerializeWithOpts(o, opts);
  public static string SerializeWithOpts(object o, JsonSerializerOptions overide) => JsonSerializer.Serialize(o, overide);
  
  public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, opts) ?? throw new Exception();

  internal class ValidStringJsonConverter<T> : JsonConverter<T> where T : ValidString {
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        (T) (Activator.CreateInstance(typeof(T), reader.GetString()) ?? throw new Exception());
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
  }
}