using Centazio.Core;

namespace centazio.core.tests.Misc;

public class DateSerialisationTests {

  [Test] public void Test_serialisation_of_dates() {
    var now = UtcDate.Utc.Now;
    var parsed = DateTime.Parse($"{now:o}").ToUniversalTime();
    
    Assert.That(parsed, Is.EqualTo(now));
    Assert.That($"{parsed:o}", Is.EqualTo($"{now:o}"));
  }

}