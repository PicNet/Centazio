namespace Centazio.Core.Misc;

public static class AsyncUtils {

  public static async Task ConditionalDelay(Func<bool> condition, int pollms=25, int maxdelayms=1000) {
    var start = DateTime.UtcNow;
    while (condition()) {
      await Task.Delay(pollms);
      var duration = (DateTime.UtcNow - start).TotalMilliseconds; 
      if (duration > maxdelayms) throw new Exception($"ConditionalDelay appears to be stuck after {duration}ms");   
    }
  }

}