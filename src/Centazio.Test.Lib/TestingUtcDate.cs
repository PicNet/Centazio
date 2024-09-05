using Centazio.Core;

namespace Centazio.Test.Lib;

public class TestingUtcDate(DateTime? start = null) : AbstractUtcDate {
  private DateTime now = start ?? TestingDefaults.DefaultStartDt;
  
  public override DateTime Now => now;
  
  public DateTime Tick() => now = now.AddSeconds(1);
  
  public static DateTime DoTick() => ((TestingUtcDate) UtcDate.Utc).Tick();
}

public class TestingIncrementingUtcDate(DateTime? start = null) : AbstractUtcDate {
  private DateTime now = start ?? TestingDefaults.DefaultStartDt;
  
  public override DateTime Now {
    get {
      var current = now; 
      now = now.AddSeconds(1);
      return current;
    }
  }
  
  public DateTime NowNoIncrement => now;

}