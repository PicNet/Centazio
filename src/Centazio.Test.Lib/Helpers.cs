using System.Text.Json;
using System.Text.Json.Serialization;
using Centazio.Core;

namespace Centazio.Test.Lib;

public static class Helpers {
  public static void DebugWriteObj(object obj, int padding = 0) => DebugWrite(JsonSerializer.Serialize(obj, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true }), padding);
  public static void DebugWrite(string msg, int padding = 0) {
    var padstr = new String('\n', padding);
    Console.WriteLine($"{padstr}{msg}{padstr}");
  }

  public static string SecsDiff(DateTime? dt = null) => ((int) ((dt ?? UtcDate.UtcNow) - TestingDefaults.DefaultStartDt).TotalSeconds).ToString();

  public static string TestingChecksum(string data) => data.GetHashCode().ToString();
  public static string TestingChecksum(object obj) => TestingChecksum(JsonSerializer.Serialize(obj));
}