using Centazio.Core;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Test.Lib;

public static class GlobalTestSuiteInitialiser {
  
  public static void Init() {
    UtcDate.Utc = new TestingUtcDate();
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    FunctionConfigDefaults.ThrowExceptions = true;
  }
}