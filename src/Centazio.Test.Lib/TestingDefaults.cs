namespace Centazio.Test.Lib;

public static class TestingDefaults {

  public static DateTime DefaultStartDt = new DateTime(2024, 1, 1, 0, 0, 0).ToUniversalTime();
  public static readonly ValidCron CRON_EVERY_SECOND = CronExpressionsHelper.EverySecond();

}