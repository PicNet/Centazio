using Centazio.Core;
using centazio.core.Ctl.Entities;
using Serilog;

namespace centazio.core.tests.Misc;

public class LoggingTests {

  public LoggingTests() {
    
    Log.Logger = LogInitialiser.GetConsoleConfig()
        // .Destructure.ByTransforming<ValidString>(obj => obj.Value) // does not work
        // .Destructure.ByTransforming<SystemName>(obj => obj.Value)  // works but requires all types to be added individually
        // .Destructure.ByTransformingWhere<ValidString>(typeof(ValidString).IsAssignableFrom, obj => obj.Value) // this works, moved to LogInitialiser
        .WriteTo.Console(LogInitialiser.Formatter)
        .MinimumLevel.Debug()
        .CreateLogger();
  }
  
  [Test] public void Test_overriding_SystemName_ToString() {
    var sysname = new SystemName(nameof(LoggingTests));
    Log.Information("system [{System}]", sysname);
  }

  [Test] public void Test_destructing_SystemName() {
    var sysname = new SystemName(nameof(LoggingTests));
    Log.Information("system [{@System}]", sysname);
  }
  
  [Test] public void Test_destructing_SystemState() {
    var sysempty = new SystemState(nameof(LoggingTests), nameof(LoggingTests), true, UtcDate.UtcNow);
    var sysfull = new SystemState(nameof(LoggingTests), nameof(LoggingTests), true, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow);
    Log.Information("empty[{@SysEmpty}] full[{@SysFull}]", sysempty, sysfull);
  }
}