namespace Centazio.Core.Misc;

public static class CronExpressionsHelper {

  public static ValidCron EveryXSeconds(int x) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, 60);
    return new ValidCron($"*/{x} * * * * *");
  }

  public static ValidCron EveryXMinutes(int x) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, 60);
    return new ValidCron($"0 */{x} * * * *");
  }

  public static ValidCron EverySecond()  => new("* * * * * *");
}