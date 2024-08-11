using Serilog;

namespace Centazio.Test.Lib;

public class TestSuiteInitialiser {
  public static void Initialise() {
    Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();
  }
}