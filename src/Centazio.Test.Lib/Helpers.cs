using System.Text.Json;
using System.Text.Json.Serialization;
using Centazio.Core;
using Centazio.Core.Checksum;
using NUnit.Framework;

namespace Centazio.Test.Lib;

public static class Helpers {
  public static void DebugWriteObj(object obj, int padding = 0) => DebugWrite(JsonSerializer.Serialize(obj, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true }), padding);
  public static void DebugWrite(string msg, int padding = 0) {
    var padstr = new String('\n', padding);
    TestContext.WriteLine($"{padstr}{msg}{padstr}");
  }

  public static string SecsDiff(DateTime? dt = null) => ((int) ((dt ?? UtcDate.UtcNow) - TestingDefaults.DefaultStartDt).TotalSeconds).ToString();

  public static StagedEntityChecksum TestingStagedEntityChecksum(string data) => new (data.GetHashCode().ToString());
  public static CoreEntityChecksum TestingCoreEntityChecksum(object obj) => new (JsonSerializer.Serialize(obj).GetHashCode().ToString());
  public static SystemEntityChecksum TestingSystemEntityChecksum(object obj) => new (JsonSerializer.Serialize(obj).GetHashCode().ToString());
}