namespace Centazio.Core.Tests.Misc;

public class CronExpressionsHelperTests {

  private readonly DateTime last = new(2020, 1, 1, 13, 50, 0, DateTimeKind.Utc);
  
  [Test] public void Test_EverySecond_success() {
    var c = CronExpressionsHelper.EverySecond();
    Assert.That(c.Value.GetNextOccurrence(last), Is.EqualTo(last.AddSeconds(1)));
  }
  
  [Test] public void Test_EveryXSeconds_arg_validation() {
    Assert.Throws<ArgumentOutOfRangeException>(() => CronExpressionsHelper.EveryXSeconds(0));
    Assert.Throws<ArgumentOutOfRangeException>(() => CronExpressionsHelper.EveryXSeconds(60));
  }
  
  [Test] public void Test_EveryXSeconds_success() {
    var c = CronExpressionsHelper.EveryXSeconds(10);
    Assert.That(c.Value.GetNextOccurrence(last), Is.EqualTo(last.AddSeconds(10)));
  }

  [Test] public void Test_EveryXMinutes_arg_validation() {
    Assert.Throws<ArgumentOutOfRangeException>(() => CronExpressionsHelper.EveryXMinutes(0));
    Assert.Throws<ArgumentOutOfRangeException>(() => CronExpressionsHelper.EveryXMinutes(60));
  }
  
  [Test] public void Test_EveryXMinutes_success() {
    var c = CronExpressionsHelper.EveryXMinutes(10);
    Assert.That(c.Value.GetNextOccurrence(last), Is.EqualTo(last.AddMinutes(10)));
  }
}