using Centazio.Core.Misc;
using Cronos;

namespace Centazio.Core.Tests.Misc;

public class CronExpressionsHelperTests {

  [Test] public void Test_EverySecond_success() {
    var (c, last) = (CronExpressionsHelper.EverySecond(), UtcDate.UtcNow);
    Assert.That(c.Value.GetNextOccurrence(last), Is.EqualTo(last.AddSeconds(1)));
  }
  
  [Test] public void Test_EveryXSeconds_arg_validation() {
    Assert.Throws<CronFormatException>(() => CronExpressionsHelper.EveryXSeconds(0));
    Assert.Throws<CronFormatException>(() => CronExpressionsHelper.EveryXSeconds(60));
  }
  
  [Test] public void Test_EveryXSeconds_success() {
    var (c, last) = (CronExpressionsHelper.EveryXSeconds(10), UtcDate.UtcNow);
    Assert.That(c.Value.GetNextOccurrence(last), Is.EqualTo(last.AddSeconds(10)));
  }

  [Test] public void Test_EveryXMinutes_arg_validation() {
    Assert.Throws<CronFormatException>(() => CronExpressionsHelper.EveryXMinutes(0));
    Assert.Throws<CronFormatException>(() => CronExpressionsHelper.EveryXMinutes(60));
  }
  
  [Test] public void Test_EveryXMinutes_success() {
    var (c, last) = (CronExpressionsHelper.EveryXMinutes(10), UtcDate.UtcNow);
    Assert.That(c.Value.GetNextOccurrence(last), Is.EqualTo(last.AddMinutes(10)));
  }
}