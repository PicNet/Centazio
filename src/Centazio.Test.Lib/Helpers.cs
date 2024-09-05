using System.Text.Json;
using System.Text.Json.Serialization;
using Centazio.Core;
using NUnit.Framework;

namespace Centazio.Test.Lib;


public static class Helpers {
  public static void DebugWriteObj(object obj) => DebugWrite(JsonSerializer.Serialize(obj, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true }));
  public static void DebugWrite(string msg) => TestContext.Out.WriteLine($"\n\n{msg}\n\n");
  public static string SecsDiff(DateTime? dt = null) => ((int) ((dt ?? UtcDate.UtcNow) - TestingDefaults.DefaultStartDt).TotalSeconds).ToString();

}