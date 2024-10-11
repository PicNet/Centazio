using System.Text.Json;
using System.Text.Json.Serialization;

namespace Centazio.Core;

// todo: add unit test to make sure JsonSerializer is no longer used
public static class Json {
  
  private static readonly JsonSerializerOptions opts = CreateOpts();
  
  private static JsonSerializerOptions CreateOpts() {
    var o = new JsonSerializerOptions();
    ValidString.AllSubclasses().ForEach(t => {
      var convtyp = typeof(ValidStringJsonConverter<>).MakeGenericType(t);
      var converter = Activator.CreateInstance(convtyp) ?? throw new Exception();
      o.Converters.Add((JsonConverter) converter);
    });
    return o;
  }

  public static string Serial(object o) => JsonSerializer.Serialize(o, opts);
  public static T Deserial<T>(string json) => JsonSerializer.Deserialize<T>(json, opts) ?? throw new Exception();

  internal class ValidStringJsonConverter<T> : JsonConverter<T> where T : ValidString {
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        (T) (Activator.CreateInstance(typeof(T), reader.GetString()) ?? throw new Exception());
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
  }
}