using Centazio.Core;

namespace Centazio.Test.Lib;

public class TestingUtcDate : AbstractUtcDate {
  private DateTime now = TestingDefaults.DefaultStartDt;
  
  public override DateTime Now => now;
  
  public void Tick() => now = now.AddMilliseconds(1);
}