using Centazio.Core;
using centazio.core.Ctl.Entities;
using Serilog;

namespace centazio.core.tests.Misc;

public class LoggingTests {

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