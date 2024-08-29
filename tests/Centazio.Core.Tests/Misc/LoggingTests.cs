using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.Tests.Misc;

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
    var sysempty = new SystemState(nameof(LoggingTests), nameof(LoggingTests), true, UtcDate.UtcNow, ESystemStateStatus.Idle);
    var sysfull = new SystemState(nameof(LoggingTests), nameof(LoggingTests), true, UtcDate.UtcNow, ESystemStateStatus.Idle, UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow);
    Log.Information("empty[{@SysEmpty}] full[{@SysFull}]", sysempty, sysfull);
  }
  
  [Test] public void Test_logging_of_exceptions() {
    try { throw new Exception(); }
    catch (Exception ex) {
      Log.Warning(ex, "logging with exception override");
      Log.Warning("logging with exception as parameter {@Exception}", ex);
    }
  }
}