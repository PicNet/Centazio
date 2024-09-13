using Centazio.Core;

namespace Centazio.Test.Lib;

public class TestingUtcDate(DateTime? start = null) : AbstractUtcDate {
  private DateTime now = start ?? TestingDefaults.DefaultStartDt;
  
  public override DateTime Now => now;
  
  public DateTime Tick() => now = now.AddSeconds(1);
  
  public static DateTime DoTick() => ((TestingUtcDate) UtcDate.Utc).Tick();
}

public class ShortLivedUtcDateOverride : IDisposable {
  private readonly IUtcDate original;
  
  public ShortLivedUtcDateOverride(DateTime dtoverride) {
    original = UtcDate.Utc;
    UtcDate.Utc = new TestingUtcDate(dtoverride);
  }
  
  public void Dispose() => UtcDate.Utc = original; 
}