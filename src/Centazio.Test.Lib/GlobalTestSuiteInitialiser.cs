using NUnit.Framework;
using Serilog;

namespace Centazio.Test.Lib;

public static class GlobalTestSuiteInitialiser {
  
  public static void Init() {
    Environment.SetEnvironmentVariable("IS_UNIT_TEST", "true");
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    DevelDebug.TargetWriteLine = TestContext.WriteLine;
  }
}