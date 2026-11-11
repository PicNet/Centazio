namespace Centazio.Core.Tests.Misc;

public class DateSerialisationTests {

  [Test] public void Test_serialisation_of_dates() {
    var now = TestingDefaults.DefaultStartDt;
    var parsed = DateTime.Parse($"{now:o}").ToUniversalTime();
    
    Assert.That(parsed, Is.EqualTo(now));
    Assert.That($"{parsed:o}", Is.EqualTo($"{now:o}"));
  }

}