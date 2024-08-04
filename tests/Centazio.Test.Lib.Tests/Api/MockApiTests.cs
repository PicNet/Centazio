using Centazio.Test.Lib.Api;

namespace Centazio.Test.Lib.Tests.Api;

public class MockApiTests {

  [Test] public void TestingMethod() {
    Console.WriteLine("1");
    new MockApi().Initialise();
  }

}