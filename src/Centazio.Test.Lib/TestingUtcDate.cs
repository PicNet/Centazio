using Centazio.Core;

namespace Centazio.Test.Lib;

public class TestingUtcDate(DateTime? start = null) : AbstractUtcDate {
  private DateTime now = start ?? TestingDefaults.DefaultStartDt;
  
  public override DateTime Now => now;
  
  public DateTime Tick(TimeSpan? interval = null) => now = now.Add(interval ?? TimeSpan.FromSeconds(1));
  
  public static DateTime DoTick(TimeSpan? interval = null) => ((TestingUtcDate) UtcDate.Utc).Tick(interval);
}

public class ShortLivedUtcDateOverride : IDisposable {
  private readonly IUtcDate original;
  
  public ShortLivedUtcDateOverride(DateTime dtoverride) {
    original = UtcDate.Utc;
    UtcDate.Utc = new TestingUtcDate(dtoverride);
  }
  
  public void Dispose() => UtcDate.Utc = original; 
}