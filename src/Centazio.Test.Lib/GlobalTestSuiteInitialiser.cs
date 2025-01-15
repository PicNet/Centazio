using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using NUnit.Framework;
using Serilog;

namespace Centazio.Test.Lib;

public static class GlobalTestSuiteInitialiser {
  
  public static void Init() {
    UtcDate.Utc = new TestingUtcDate();
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    DevelDebug.TargetWriteLine = TestContext.WriteLine;
    FunctionConfigDefaults.ThrowExceptions = true;
  }
}