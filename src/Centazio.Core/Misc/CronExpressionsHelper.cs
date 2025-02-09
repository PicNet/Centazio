namespace Centazio.Core.Misc;

public static class CronExpressionsHelper {

  public static ValidCron EveryXSeconds(int x) => new($"*/{x} * * * * *");
  public static ValidCron EveryXMinutes(int x) => new($"0 */{x} * * * *");
  public static ValidCron EverySecond()  => new("* * * * * *");

}