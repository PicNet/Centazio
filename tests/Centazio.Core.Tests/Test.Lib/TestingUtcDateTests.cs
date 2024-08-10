using Centazio.Test.Lib;

namespace centazio.core.tests.Test.Lib;

public class TestingUtcDateTests {

  [Test] public void TestTicksChange() {
    var utc = new TestingUtcDate();
    var ticks = Enumerable.Range(0, 100).Select(_ => utc.Now.Ticks);
    var unique = ticks.Distinct().ToList();
    Assert.That(unique, Has.Count.EqualTo(100));
  }
}