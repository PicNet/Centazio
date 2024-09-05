using Centazio.Core;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib;

public static class GlobalTestSuiteInitialiser {
  
  public static void Init() {
    UtcDate.Utc = new TestingUtcDate();
    Log.Logger = LogInitialiser.GetConsoleConfig(LogEventLevel.Fatal).CreateLogger();
  }
}