namespace Centazio.Test.Lib;

public static class TestingDefaults {

  public static DateTime DefaultStartDt = new DateTime(2024, 1, 1, 0, 0, 0).ToUniversalTime();
  public static readonly ValidCron CRON_EVERY_SECOND = CronExpressionsHelper.EverySecond();
  // 4.14.0 is the last LocalStack release that runs without a licence
  public const string LOCALSTACK_IMAGE = "localstack/localstack:4.14.0@sha256:3ebc37595918b8accb852f8048fef2aff047d465167edd655528065b07bc364a";

}