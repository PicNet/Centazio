using Centazio.Core;

namespace Centazio.Test.Lib;

public class TestingUtcDate(DateTime? now = null) : AbstractUtcDate {
  private DateTime now = now ?? TestingDefaults.DefaultStartDt;
  
  public override DateTime Now => now;
  
  public DateTime Tick() => now = now.AddMilliseconds(1);
}