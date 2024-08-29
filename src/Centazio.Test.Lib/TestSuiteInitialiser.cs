using Centazio.Core;
using Serilog;

namespace Centazio.Test.Lib;

public class TestSuiteInitialiser {
  public static void Initialise(IUtcDate? utc=null) {
    UtcDate.Utc = utc ?? new TestingUtcDate();
    
    Log.Logger = LogInitialiser.GetConsoleConfig()
        .CreateLogger();
  }
}