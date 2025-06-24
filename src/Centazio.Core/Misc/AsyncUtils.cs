namespace Centazio.Core.Misc;

public static class AsyncUtils {

  public static async Task ConditionalDelay(Func<bool> condition, int delayms=25, int maxiterations=20) {
    var iter = 0;
    while (condition()) {
      await Task.Delay(delayms);
      if (++iter > maxiterations) throw new Exception($"ConditionalDelay appears to be stuck after {iter} iterations of {delayms}ms delays");   
    }
  }

}